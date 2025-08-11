using System.Text;
using System.Text.Json;                       // JSON health writer
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Diagnostics.HealthChecks; // HealthCheckOptions
using Microsoft.OpenApi.Models;               // Swagger security types
using Prometheus;

using DotNetEnv;                              // .env loader
using System.Text.RegularExpressions;
using Npgsql;                                 // NpgsqlConnectionStringBuilder

using BudgetForge.Application.Mapping;
using BudgetForge.Application.Models;
using BudgetForge.Application.Services;
using BudgetForge.Application.Interfaces;
using BudgetForge.Infrastructure.Data;
using BudgetForge.Infrastructure.Services;
using BudgetForge.Infrastructure.Identity;     // AppUser

using FluentValidation;
using FluentValidation.AspNetCore;
using BudgetForge.Application.Validators;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// Load .env explicitly from the repo root (dotnet watch runs
// with content root at src/BudgetForge.Api, so default Env.Load()
// may not find the file at the solution root).
// ---------------------------------------------------------
var contentRoot = builder.Environment.ContentRootPath;               // .../src/BudgetForge.Api
var solutionRoot = Path.GetFullPath(Path.Combine(contentRoot, "..", ".."));
Env.Load(Path.Combine(solutionRoot, ".env"));

// (Optional) Keep your placeholder expansion logic if you want to support ${VAR} in appsettings.json.
// Not required anymore for DB/JWT because we read env directly below.
var entries = builder.Configuration.AsEnumerable().ToList();
foreach (var kv in entries)
{
    if (kv.Value is null) continue;
    var expanded = Regex.Replace(kv.Value, @"\$\{([A-Z0-9_]+)\}",
        m => Environment.GetEnvironmentVariable(m.Groups[1].Value) ?? m.Value,
        RegexOptions.IgnoreCase);
    if (!ReferenceEquals(expanded, kv.Value))
        builder.Configuration[kv.Key] = expanded;
}

// ---------------------------------------------------------
// Helper: build the Postgres connection string from env vars
// (fallback to appsettings if env missing). This avoids ${...}
// parsing issues and ensures Port is a valid int.
// ---------------------------------------------------------
string BuildConnectionString()
{
    var host = Environment.GetEnvironmentVariable("DB_HOST");
    var db   = Environment.GetEnvironmentVariable("DB_NAME");
    var user = Environment.GetEnvironmentVariable("DB_USER");
    var pass = Environment.GetEnvironmentVariable("DB_PASS");
    var portVar = Environment.GetEnvironmentVariable("DB_PORT");

    if (!string.IsNullOrWhiteSpace(host) &&
        !string.IsNullOrWhiteSpace(db)   &&
        !string.IsNullOrWhiteSpace(user) &&
        !string.IsNullOrWhiteSpace(pass) &&
        !string.IsNullOrWhiteSpace(portVar) &&
        int.TryParse(portVar, out var port))
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Database = db,
            Username = user,
            Password = pass,
            Port = port
        };
        return csb.ToString();
    }

    // Fallback to whatever is in appsettings.json
    return builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("No connection string configured.");
}

var builtConnString = BuildConnectionString(); // reuse in EF + health checks

// Add services to the container.
builder.Services.AddControllers();

// Mapping Profile 
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Register FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTransactionRequestValidator>();

// EF Core (PostgreSQL) — use the built connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builtConnString));

// JWT Settings (bind from config, then override from env if present)
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are not properly configured");

// Override from env if provided (best practice for secrets)
jwtSettings.SecretKey = Environment.GetEnvironmentVariable("JWT_SECRET")   ?? jwtSettings.SecretKey;
jwtSettings.Issuer    = Environment.GetEnvironmentVariable("JWT_ISSUER")   ?? jwtSettings.Issuer;
jwtSettings.Audience  = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? jwtSettings.Audience;

if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
    throw new InvalidOperationException("JWT SecretKey is missing.");

// JWT Token Service (keep for now; can remove once AuthController fully uses Identity)
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Core Services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

// Metrics Service
builder.Services.AddSingleton<IMetricsService, MetricsService>();

// ASP.NET Core Identity (users/roles) + EF store
builder.Services
    .AddIdentityCore<AppUser>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 8;
        o.Password.RequireDigit = true;
        o.Password.RequireLowercase = true;
        o.Password.RequireUppercase = true;
        o.Password.RequireNonAlphanumeric = true;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddRoles<IdentityRole<int>>()                     // role support
    .AddEntityFrameworkStores<ApplicationDbContext>() // persist Identity in our DbContext
    .AddSignInManager()                                // password/lockout checks
    .AddDefaultTokenProviders();                       // email/2FA/reset tokens

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = jwtSettings.SaveToken;
    options.RequireHttpsMetadata = jwtSettings.RequireHttpsMetadata;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateIssuer = jwtSettings.ValidateIssuer,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = jwtSettings.ValidateAudience,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = jwtSettings.ValidateLifetime,
        ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkewSeconds)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
                context.Response.Headers.Append("Token-Expired", "true");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new
            {
                error = "You are not authorized",
                details = context.ErrorDescription
            });
            return context.Response.WriteAsync(result);
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new
            {
                error = "You do not have permission to access this resource"
            });
            return context.Response.WriteAsync(result);
        }
    };
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3001") // add http://localhost:4200 later for Angular
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Swagger (with JWT Bearer auth so the Authorize button works)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BudgetForge API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer' [space] then your JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { scheme, Array.Empty<string>() }
    });
});

// ---------- Health checks (use the same built connection) ----------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(name: "ef-dbcontext")
    .AddNpgSql(builtConnString, name: "npgsql-connection");
// -------------------------------------------------------------------

var app = builder.Build();

// Seed Identity roles ("Admin", "User") at startup
using (var scope = app.Services.CreateScope())
{
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    foreach (var r in new[] { "Admin", "User" })
        if (!await roleMgr.RoleExistsAsync(r))
            await roleMgr.CreateAsync(new IdentityRole<int>(r));
}

// Prometheus metrics BEFORE other middleware
app.UseHttpMetrics();

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Endpoint mapping
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            results = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                error = e.Value.Exception?.Message,
                data = e.Value.Data
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
});

app.MapMetrics();

// sanity endpoint
app.MapGet("/ping", () => Results.Text("pong", "text/plain"));
app.Run();