using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using DotNetEnv;
using Npgsql;

using BudgetForge.Application.Models; // JwtSettings
using BudgetForge.Infrastructure.Data;
using BudgetForge.Infrastructure.Identity; // AppUser

// Application
using BudgetForge.Application.Mapping;
using BudgetForge.Infrastructure.Services; // Implementations
using BudgetForge.Application.Interfaces; // Interfaces
using BudgetForge.Application.Validators;

// Billing
using BudgetForge.Application.Interfaces.Billing;
using BudgetForge.Infrastructure.Services.Billing;

// Misc
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// Load .env from solution root (so dotnet watch finds it)
// ---------------------------------------------------------
var contentRoot = builder.Environment.ContentRootPath;               // .../src/BudgetForge.Api
var solutionRoot = Path.GetFullPath(Path.Combine(contentRoot, "..", ".."));
Env.Load(Path.Combine(solutionRoot, ".env"));

// Optional: expand ${VAR} placeholders found in config values
foreach (var kv in builder.Configuration.AsEnumerable().ToList())
{
    if (kv.Value is null) continue;
    var expanded = Regex.Replace(
        kv.Value,
        @"\$\{([A-Z0-9_]+)\}",
        m => Environment.GetEnvironmentVariable(m.Groups[1].Value) ?? m.Value,
        RegexOptions.IgnoreCase
    );
    if (!ReferenceEquals(expanded, kv.Value))
        builder.Configuration[kv.Key] = expanded;
}

// ---------------------------------------------------------
// Build Postgres connection string (env first, config fallback)
// ---------------------------------------------------------
string BuildConnectionString()
{
    var host = Environment.GetEnvironmentVariable("DB_HOST");
    var db   = Environment.GetEnvironmentVariable("DB_NAME");
    var user = Environment.GetEnvironmentVariable("DB_USER");
    var pass = Environment.GetEnvironmentVariable("DB_PASS");
    var port = Environment.GetEnvironmentVariable("DB_PORT");

    if (!string.IsNullOrWhiteSpace(host) &&
        !string.IsNullOrWhiteSpace(db) &&
        !string.IsNullOrWhiteSpace(user) &&
        !string.IsNullOrWhiteSpace(pass) &&
        int.TryParse(port, out var p))
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Database = db,
            Username = user,
            Password = pass,
            Port = p
        };
        return csb.ToString();
    }

    return builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("No connection string configured.");
}

var builtConnString = BuildConnectionString();

// ---------------------------------------------------------
// Services
// ---------------------------------------------------------
builder.Services.AddControllers();

// AutoMapper / Validation
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTransactionRequestValidator>();

// EF Core (PostgreSQL)
builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(builtConnString));

// Identity
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
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// JWT settings: bind from config, then overlay env vars into the options
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.PostConfigure<JwtSettings>(opts =>
{
    var envSecret  = Environment.GetEnvironmentVariable("JWT_SECRET");
    var envIssuer  = Environment.GetEnvironmentVariable("JWT_ISSUER");
    var envAudience= Environment.GetEnvironmentVariable("JWT_AUDIENCE");
    if (!string.IsNullOrWhiteSpace(envSecret))   opts.SecretKey = envSecret;
    if (!string.IsNullOrWhiteSpace(envIssuer))   opts.Issuer = envIssuer;
    if (!string.IsNullOrWhiteSpace(envAudience)) opts.Audience = envAudience;
});

// Build a merged instance for configuring JwtBearer right now
var jwt = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
var envSecretNow   = Environment.GetEnvironmentVariable("JWT_SECRET");
var envIssuerNow   = Environment.GetEnvironmentVariable("JWT_ISSUER");
var envAudienceNow = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
if (!string.IsNullOrWhiteSpace(envSecretNow))   jwt.SecretKey = envSecretNow;
if (!string.IsNullOrWhiteSpace(envIssuerNow))   jwt.Issuer = envIssuerNow;
if (!string.IsNullOrWhiteSpace(envAudienceNow)) jwt.Audience = envAudienceNow;

if (string.IsNullOrWhiteSpace(jwt.SecretKey))
    throw new InvalidOperationException("JWT SecretKey is missing.");

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = jwt.SaveToken;
    options.RequireHttpsMetadata = jwt.RequireHttpsMetadata;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = jwt.ValidateIssuerSigningKey,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
        ValidateIssuer = jwt.ValidateIssuer,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = jwt.ValidateAudience,
        ValidAudience = jwt.Audience,
        ValidateLifetime = jwt.ValidateLifetime,
        ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds)
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
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", p => p.RequireRole("User", "Admin"));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3001", "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Swagger (JWT enabled)
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
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(name: "ef-dbcontext")
    .AddNpgSql(builtConnString, name: "npgsql-connection");

// App services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();

// Billing services
builder.Services.AddScoped<IBillQueries, BillQueries>();
builder.Services.AddScoped<IBillCommands, BillCommands>();

var app = builder.Build();

// Seed Identity roles
using (var scope = app.Services.CreateScope())
{
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    foreach (var r in new[] { "Admin", "User" })
        if (!await roleMgr.RoleExistsAsync(r))
            await roleMgr.CreateAsync(new IdentityRole<int>(r));
}

// Prometheus first
app.UseHttpMetrics();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();

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
app.MapGet("/ping", () => Results.Text("pong", "text/plain"));

app.Run();