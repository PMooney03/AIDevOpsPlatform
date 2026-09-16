using DevOps.Application.Abstractions;
using DevOps.Infrastructure.Monitoring;
using DevOps.Infrastructure.Persistence;
using DevOps.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevOps.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. Set ConnectionStrings__DefaultConnection.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHttpClient<IHttpHealthChecker, HttpHealthChecker>(client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddScoped<IMonitoredServiceRepository, MonitoredServiceRepository>();
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<IHealthCheckResultRepository, HealthCheckResultRepository>();
        services.AddScoped<IAiAnalysisRepository, AiAnalysisRepository>();
        services.AddScoped<IDeploymentRepository, DeploymentRepository>();
        services.AddScoped<IRemediationRepository, RemediationRepository>();
        services.AddSingleton<IContainerMonitor, DockerContainerMonitor>();
        return services;
    }
}

