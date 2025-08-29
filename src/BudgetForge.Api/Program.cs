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

using StackExchange.Redis;

using BudgetForge.Application.Models;          // JwtSettings
using BudgetForge.Infrastructure.Data;
using BudgetForge.Infrastructure.Identity;    // AppUser

// Application
using BudgetForge.Application.Mapping;
using BudgetForge.Infrastructure.Services;     // Implementations
using BudgetForge.Application.Interfaces;      // Interfaces
using BudgetForge.Application.Validators;

// Billing
using BudgetForge.Application.Interfaces.Billing;
using BudgetForge.Infrastructure.Services.Billing;

// Misc
using FluentValidation;
using FluentValidation.AspNetCore;



var builder = WebApplication.CreateBuilder(args);

// Redis connection (robust, with sane fallback for docker-compose)
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var cfg =
        builder.Configuration.GetConnectionString("Redis") ??
        Environment.GetEnvironmentVariable("ConnectionStrings__Redis") ??
        "budgetforge-redis:6379"; // falls back to container name on the compose network

    return ConnectionMultiplexer.Connect(cfg);
});
// Redis cache abstraction
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// ---------------------------------------------------------
// Load .env when running locally (solution root)
// ---------------------------------------------------------
var contentRoot = builder.Environment.ContentRootPath;
var solutionRoot = Path.GetFullPath(Path.Combine(contentRoot, "..", ".."));
if (File.Exists(Path.Combine(solutionRoot, ".env")))
{
    Env.Load(Path.Combine(solutionRoot, ".env"));
}

// Expand ${VAR} placeholders
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
// Build Postgres connection string
// ---------------------------------------------------------
string BuildConnectionString()
{
    var envConn =
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
        Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION");
    if (!string.IsNullOrWhiteSpace(envConn))
        return envConn;

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

    var cfgConn = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(cfgConn))
        return cfgConn;

    throw new InvalidOperationException("No database connection string is configured.");
}

var connectionString = BuildConnectionString();
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// ---------------------------------------------------------
// Services
// ---------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTransactionRequestValidator>();
builder.Services.AddScoped<IBudgetSummaryService, BudgetSummaryService>();

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(connectionString, npg => npg.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null))
);

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

builder.Services.AddScoped<IPasswordHasher<AppUser>, BudgetForge.Infrastructure.Identity.Argon2IdPasswordHasher<AppUser>>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.PostConfigure<JwtSettings>(opts =>
{
    var envSecret   = Environment.GetEnvironmentVariable("JWT_SECRET");
    var envIssuer   = Environment.GetEnvironmentVariable("JWT_ISSUER");
    var envAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
    if (!string.IsNullOrWhiteSpace(envSecret))   opts.SecretKey = envSecret;
    if (!string.IsNullOrWhiteSpace(envIssuer))   opts.Issuer = envIssuer;
    if (!string.IsNullOrWhiteSpace(envAudience)) opts.Audience = envAudience;
});

var jwt = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
{
    var envSecret   = Environment.GetEnvironmentVariable("JWT_SECRET");
    var envIssuer   = Environment.GetEnvironmentVariable("JWT_ISSUER");
    var envAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
    if (!string.IsNullOrWhiteSpace(envSecret))   jwt.SecretKey = envSecret;
    if (!string.IsNullOrWhiteSpace(envIssuer))   jwt.Issuer = envIssuer;
    if (!string.IsNullOrWhiteSpace(envAudience)) jwt.Audience = envAudience;
}
if (string.IsNullOrWhiteSpace(jwt.SecretKey))
    throw new InvalidOperationException("JWT SecretKey is missing.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken            = jwt.SaveToken;
    options.RequireHttpsMetadata = jwt.RequireHttpsMetadata;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = jwt.ValidateIssuerSigningKey,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
        ValidateIssuer           = jwt.ValidateIssuer,
        ValidIssuer              = jwt.Issuer,
        ValidateAudience         = jwt.ValidateAudience,
        ValidAudience            = jwt.Audience,
        ValidateLifetime         = jwt.ValidateLifetime,
        ClockSkew                = TimeSpan.FromSeconds(jwt.ClockSkewSeconds)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy("AdminOnly",   p => p.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", p => p.RequireRole("User", "Admin"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:3000",
                  "https://localhost:3001",
                  "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BudgetForge API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Description  = "Enter 'Bearer' [space] then your JWT",
        In           = ParameterLocation.Header,
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        Reference    = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(name: "ef-dbcontext")
    .AddNpgSql(connectionString, name: "npgsql-connection")
    .AddRedis(builder.Configuration.GetConnectionString("Redis")
              ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis")
              ?? "budgetforge-redis:6379",
              name: "redis");

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();
builder.Services.AddScoped<IBillQueries, BillQueries>();
builder.Services.AddScoped<IBillCommands, BillCommands>();

var app = builder.Build();

// enable swagger in dev OR when ENABLE_SWAGGER=true OR config EnableSwagger=true
var enableSwagger =
    app.Environment.IsDevelopment() ||
    string.Equals(Environment.GetEnvironmentVariable("ENABLE_SWAGGER"), "true", StringComparison.OrdinalIgnoreCase) ||
    builder.Configuration.GetValue<bool>("EnableSwagger", false);

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BudgetForge API v1");
    });
}

// migrate + seed in background
async Task MigrateAndSeedAsync(IServiceProvider services, ILogger logger, CancellationToken ct)
{
    const int maxRetries = 10;
    var delay = TimeSpan.FromSeconds(2);

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            await db.Database.MigrateAsync(ct);

            foreach (var r in new[] { "Admin", "User" })
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole<int>(r));

            logger.LogInformation("DB migrate + seed succeeded on attempt {Attempt}", attempt);
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DB not ready; attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
            if (attempt == maxRetries)
            {
                logger.LogError(ex, "Giving up on DB init; API will continue without seed.");
                return;
            }
            await Task.Delay(delay, ct);
        }
    }
}

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
_ = Task.Run(() => MigrateAndSeedAsync(app.Services, startupLogger, CancellationToken.None));

app.UseHttpMetrics();
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
            status  = report.Status.ToString(),
            results = report.Entries.Select(e => new
            {
                name   = e.Key,
                status = e.Value.Status.ToString(),
                error  = e.Value.Exception?.Message,
                data   = e.Value.Data
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
});

app.MapMetrics();
app.MapGet("/ping", () => Results.Text("pong", "text/plain"));
app.Run();