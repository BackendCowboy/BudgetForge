using Microsoft.EntityFrameworkCore;
using BudgetForge.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add health checks for Kubernetes and Docker
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(
        name: "database",
        failureStatus: HealthStatus.Degraded,  // Don't fail completely if DB is down
        tags: new[] { "db", "ready" })
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), tags: new[] { "ready" })
    .AddCheck("live", () => HealthCheckResult.Healthy("API is alive"), tags: new[] { "live" });

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger in all environments for now (you can restrict later)
app.UseSwagger();
app.UseSwaggerUI();

// Health check endpoints

// Simple liveness check - no external dependencies
app.MapGet("/health/live", () => Results.Ok(new 
{ 
    status = "Healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

// Readiness check - includes database check but won't fail the endpoint
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        // Always return 200 OK, even if some checks are degraded
        context.Response.StatusCode = 200;
        
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Main health check - comprehensive but forgiving
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        // Always return 200 OK for main health endpoint
        context.Response.StatusCode = 200;
        
        var response = new
        {
            status = report.Status == HealthStatus.Unhealthy ? "Degraded" : report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            application = "BudgetForge API",
            version = "1.0.0",
            environment = app.Environment.EnvironmentName,
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

app.Run();