using DevOps.Application.Abstractions;
using DevOps.Application.Analysis;
using DevOps.Application.Monitoring;
using DevOps.Application.Observability;
using DevOps.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DevOps.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MonitoringOptions>(configuration.GetSection(MonitoringOptions.SectionName));
        services.Configure<DockerOptions>(configuration.GetSection(DockerOptions.SectionName));
        services.TryAddSingleton<IContainerMonitor, DisabledContainerMonitor>();
        services.TryAddSingleton<IHttpHealthChecker, UnavailableHttpHealthChecker>();
        services.AddSingleton<IPlatformMetrics, PlatformMetrics>();
        services.TryAddSingleton<IIncidentAnalyzer, UnavailableIncidentAnalyzer>();
        services.AddScoped<MonitoredServiceService>();
        services.AddScoped<IncidentService>();
        services.AddScoped<IncidentAnalysisService>();
        services.AddScoped<DeploymentService>();
        services.AddScoped<RemediationService>();
        services.AddScoped<OverviewService>();
        services.AddScoped<ServiceHealthQueryService>();
        services.AddScoped<IServiceHealthMonitor, ServiceHealthMonitor>();
        return services;
    }
}

