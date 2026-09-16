using DevOps.Application.Monitoring;
using DevOps.Application.Services;
using DevOps.Core.Entities;
using DevOps.Core.Enums;
using DevOps.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevOps.UnitTests.Monitoring;

public class ServiceHealthMonitorTests
{
    [Fact]
    public async Task Successful_check_marks_service_healthy_and_stores_result()
    {
        var (monitor, services, healthChecks, incidents, checker) = CreateSut();
        var service = await SeedServiceAsync(services);

        checker.Enqueue(new HealthProbeResult(true, true, 200, 25, "ok"));
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);

        var stored = await services.GetByIdAsync(service.Id, CancellationToken.None);
        Assert.Equal(ServiceStatus.Healthy, stored!.Status);
        Assert.Equal(0, stored.ConsecutiveFailureCount);
        Assert.NotNull(stored.LastHealthyAt);
        Assert.Single(healthChecks.Results);
        Assert.Empty(await incidents.GetAllAsync(CancellationToken.None));
        Assert.Equal(new Uri("https://payments.internal/health"), checker.RequestedUrls[0]);
    }

    [Fact]
    public async Task Failed_check_does_not_create_incident_before_threshold()
    {
        var (monitor, services, _, incidents, checker) = CreateSut();
        var service = await SeedServiceAsync(services);

        checker.Enqueue(Failed());
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);

        var stored = await services.GetByIdAsync(service.Id, CancellationToken.None);
        Assert.Equal(ServiceStatus.Degraded, stored!.Status);
        Assert.Empty(await incidents.GetAllAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Reaching_failure_threshold_creates_one_incident()
    {
        var (monitor, services, _, incidents, checker) = CreateSut();
        var service = await SeedServiceAsync(services);

        checker.Enqueue(Failed());
        checker.Enqueue(Failed());
        checker.Enqueue(Failed());
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);

        var stored = await services.GetByIdAsync(service.Id, CancellationToken.None);
        var open = await incidents.GetAllAsync(CancellationToken.None);
        Assert.Equal(ServiceStatus.Unhealthy, stored!.Status);
        Assert.Single(open);
        Assert.Equal(IncidentStatus.Open, open[0].Status);
        Assert.Equal(IncidentSeverity.High, open[0].Severity);
    }

    [Fact]
    public async Task Further_failures_update_existing_incident_instead_of_duplicating()
    {
        var (monitor, services, _, incidents, checker) = CreateSut();
        var service = await SeedServiceAsync(services);

        checker.Enqueue(Failed());
        checker.Enqueue(Failed());
        checker.Enqueue(Failed());
        checker.Enqueue(Failed());
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);

        Assert.Single(await incidents.GetAllAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Recovery_marks_healthy_without_creating_another_incident()
    {
        var (monitor, services, _, incidents, checker) = CreateSut();
        var service = await SeedServiceAsync(services);

        checker.Enqueue(Failed());
        checker.Enqueue(Failed());
        checker.Enqueue(Failed());
        checker.Enqueue(new HealthProbeResult(true, true, 200, 30, "recovered"));
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);

        var stored = await services.GetByIdAsync(service.Id, CancellationToken.None);
        Assert.Equal(ServiceStatus.Healthy, stored!.Status);
        Assert.Equal(0, stored.ConsecutiveFailureCount);
        Assert.Single(await incidents.GetAllAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Unreachable_for_configured_period_marks_offline_and_creates_critical_incident()
    {
        var (monitor, services, _, incidents, checker) = CreateSut();
        var service = await SeedServiceAsync(services);
        service.UnreachableSince = DateTimeOffset.UtcNow.AddMinutes(-5);

        checker.Enqueue(new HealthProbeResult(false, false, null, 8, "unreachable"));
        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);

        var stored = await services.GetByIdAsync(service.Id, CancellationToken.None);
        var open = await incidents.GetAllAsync(CancellationToken.None);
        Assert.Equal(ServiceStatus.Offline, stored!.Status);
        Assert.Single(open);
        Assert.Equal(IncidentSeverity.Critical, open[0].Severity);
    }

    [Fact]
    public async Task Disabled_services_are_not_checked()
    {
        var (monitor, services, healthChecks, _, checker) = CreateSut();
        var service = await SeedServiceAsync(services);
        service.MonitoringEnabled = false;

        await monitor.MonitorEnabledServicesAsync(CancellationToken.None);

        Assert.Empty(checker.RequestedUrls);
        Assert.Empty(healthChecks.Results);
    }

    [Fact]
    public async Task Health_checks_record_platform_metrics()
    {
        var services = new FakeMonitoredServiceRepository();
        var healthChecks = new FakeHealthCheckResultRepository();
        var incidents = new FakeIncidentRepository();
        var checker = new FakeHttpHealthChecker();
        var metrics = new FakePlatformMetrics();
        var monitor = new ServiceHealthMonitor(
            services,
            healthChecks,
            incidents,
            checker,
            new FakeContainerMonitor(),
            Options.Create(new MonitoringOptions { UnhealthyFailureThreshold = 3 }),
            metrics,
            NullLogger<ServiceHealthMonitor>.Instance);

        var service = await SeedServiceAsync(services);
        checker.Enqueue(Failed());
        await monitor.MonitorEnabledServicesAsync(CancellationToken.None);

        Assert.Equal(1, metrics.HealthChecks);
        Assert.Equal(1, metrics.FailedHealthChecks);
    }

    [Fact]
    public async Task Previously_running_container_that_disappears_creates_an_incident()
    {
        var services = new FakeMonitoredServiceRepository();
        var healthChecks = new FakeHealthCheckResultRepository();
        var incidents = new FakeIncidentRepository();
        var checker = new FakeHttpHealthChecker();
        var containers = new FakeContainerMonitor
        {
            Next = new ContainerSnapshot(false, "demo-unhealthy", null, null, false, null, 0, ContainerHealth.NotFound, null, null)
        };
        var monitor = new ServiceHealthMonitor(
            services,
            healthChecks,
            incidents,
            checker,
            containers,
            Options.Create(new MonitoringOptions { UnhealthyFailureThreshold = 3 }),
            new FakePlatformMetrics(),
            NullLogger<ServiceHealthMonitor>.Instance);

        var service = await SeedServiceAsync(services);
        service.ContainerName = "demo-unhealthy";
        service.ContainerRunning = true;
        service.ContainerObservedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        checker.Enqueue(new HealthProbeResult(true, true, 200, 20, "ok"));

        await monitor.CheckServiceAsync(service.Id, CancellationToken.None);

        var open = await incidents.GetAllAsync(CancellationToken.None);
        Assert.Single(open);
        Assert.Contains("container stopped", open[0].Title, StringComparison.OrdinalIgnoreCase);
    }

    private static HealthProbeResult Failed() =>
        new(true, false, 500, 12, "HTTP 500");

    private static async Task<MonitoredService> SeedServiceAsync(FakeMonitoredServiceRepository services)
    {
        var service = new MonitoredService
        {
            Id = Guid.CreateVersion7(),
            Name = "payments-api",
            BaseUrl = "https://payments.internal",
            HealthEndpoint = "/health",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await services.AddAsync(service, CancellationToken.None);
        return service;
    }

    private static (
        ServiceHealthMonitor Monitor,
        FakeMonitoredServiceRepository Services,
        FakeHealthCheckResultRepository HealthChecks,
        FakeIncidentRepository Incidents,
        FakeHttpHealthChecker Checker) CreateSut()
    {
        var services = new FakeMonitoredServiceRepository();
        var healthChecks = new FakeHealthCheckResultRepository();
        var incidents = new FakeIncidentRepository();
        var checker = new FakeHttpHealthChecker();
        var options = Options.Create(new MonitoringOptions
        {
            UnhealthyFailureThreshold = 3,
            DegradedResponseTimeMs = 1000,
            OfflineUnreachablePeriod = TimeSpan.FromMinutes(2)
        });

        var monitor = new ServiceHealthMonitor(
            services,
            healthChecks,
            incidents,
            checker,
            new FakeContainerMonitor(),
            options,
            new FakePlatformMetrics(),
            NullLogger<ServiceHealthMonitor>.Instance);

        return (monitor, services, healthChecks, incidents, checker);
    }
}
