using DevOps.Application.Abstractions;
using DevOps.Infrastructure.Persistence;
using DevOps.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevOps.IntegrationTests;

public sealed class DevOpsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"devops-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            var databaseName = _databaseName;
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
            services.AddScoped<IMonitoredServiceRepository, MonitoredServiceRepository>();
            services.AddScoped<IIncidentRepository, IncidentRepository>();
            services.AddScoped<IHealthCheckResultRepository, HealthCheckResultRepository>();
            services.AddScoped<IAiAnalysisRepository, AiAnalysisRepository>();
            services.AddScoped<IDeploymentRepository, DeploymentRepository>();
            services.AddScoped<IRemediationRepository, RemediationRepository>();
        });
    }
}
