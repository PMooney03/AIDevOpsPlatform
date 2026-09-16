using System.Text.Json.Serialization;
using DevOps.AI;
using DevOps.Api.Auth;
using DevOps.Api.Middleware;
using DevOps.Api.Observability;
using DevOps.Application;
using DevOps.Infrastructure;
using DevOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "O";
    options.UseUtcTimestamp = true;
});
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Services.AddApplication(builder.Configuration);

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddIncidentAnalysis(builder.Configuration);
}

builder.Services.AddPlatformAuth(builder.Configuration, builder.Environment);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddPlatformTelemetry();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);
if (builder.Environment.IsEnvironment("Testing"))
{
    healthChecks.AddCheck("database", () => HealthCheckResult.Healthy(), tags: ["ready"]);
}
else
{
    healthChecks.AddDbContextCheck<ApplicationDbContext>("database", tags: ["ready"]);
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<HttpRequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseCors();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DemoDataSeeder.EnsureDemoServicesAsync(db, CancellationToken.None);
}

var liveOptions = new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") };
var readyOptions = new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") };

app.MapHealthChecks("/health", liveOptions).AllowAnonymous();
app.MapHealthChecks("/health/live", liveOptions).AllowAnonymous();
app.MapHealthChecks("/health/ready", readyOptions).AllowAnonymous();
app.MapPrometheusScrapingEndpoint().AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program;
