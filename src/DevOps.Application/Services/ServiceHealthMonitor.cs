using DevOps.Application.Abstractions;
using DevOps.Application.Monitoring;
using DevOps.Core.Entities;
using DevOps.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOps.Application.Services;

public sealed class ServiceHealthMonitor(
    IMonitoredServiceRepository services,
    IHealthCheckResultRepository healthChecks,
    IIncidentRepository incidents,
    IHttpHealthChecker healthChecker,
    IContainerMonitor containers,
    IOptions<MonitoringOptions> options,
    IPlatformMetrics metrics,
    ILogger<ServiceHealthMonitor> logger) : IServiceHealthMonitor
{
    public async Task MonitorEnabledServicesAsync(CancellationToken cancellationToken)
    {
        var serviceIds = await services.GetEnabledIdsAsync(cancellationToken);
        var degree = Math.Max(1, options.Value.MaxConcurrentHealthChecks);
        using var gate = new SemaphoreSlim(degree);
        var tasks = serviceIds.Select(async serviceId =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                await CheckServiceAsync(serviceId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Health check failed for Service {ServiceId}", serviceId);
            }
            finally
            {
                gate.Release();
            }
        });
        await Task.WhenAll(tasks);

        var retentionDays = Math.Max(1, options.Value.HealthCheckRetentionDays);
        var removed = await healthChecks.DeleteOlderThanAsync(
            DateTimeOffset.UtcNow.AddDays(-retentionDays),
            cancellationToken);
        if (removed > 0)
        {
            logger.LogInformation("Removed {Count} health check rows older than {Days} days", removed, retentionDays);
        }

        var snapshot = await services.GetAllAsync(cancellationToken);
        var incidentSnapshot = await incidents.GetAllAsync(cancellationToken);
        metrics.SetInventorySnapshot(snapshot, incidentSnapshot);
    }

    public async Task CheckServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var service = await services.GetByIdAsync(serviceId, cancellationToken);
        if (service is null || !service.MonitoringEnabled)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var healthUrl = HealthUrl.Combine(service.BaseUrl, service.HealthEndpoint);
        var probe = await healthChecker.CheckAsync(healthUrl, cancellationToken);

        if (probe.ReachedHost)
        {
            service.UnreachableSince = null;
        }
        else
        {
            service.UnreachableSince ??= now;
        }

        service.ConsecutiveFailureCount = probe.IsSuccessStatusCode
            ? 0
            : service.ConsecutiveFailureCount + 1;

        var status = HealthStatusEvaluator.Evaluate(
            probe,
            service.ConsecutiveFailureCount,
            service.UnreachableSince,
            options.Value,
            now);

        service.Status = status;
        service.LastHealthCheckAt = now;
        service.UpdatedAt = now;
        if (status == ServiceStatus.Healthy)
        {
            service.LastHealthyAt = now;
        }

        var result = new HealthCheckResult
        {
            Id = Guid.CreateVersion7(),
            ServiceId = service.Id,
            Status = status,
            ResponseTimeMs = probe.ResponseTimeMs,
            HttpStatusCode = probe.HttpStatusCode,
            Message = probe.Message,
            CheckedAt = now
        };

        await healthChecks.AddAsync(result, cancellationToken);
        await ApplyIncidentAsync(service, status, probe, now, cancellationToken);
        await ObserveContainerAsync(service, now, cancellationToken);
        await healthChecks.SaveChangesAsync(cancellationToken);

        var outcome = probe.IsSuccessStatusCode ? HealthCheckOutcome.Success : HealthCheckOutcome.Failure;
        metrics.RecordHealthCheck(service.Name, service.Id, status, outcome, probe.HttpStatusCode, probe.ResponseTimeMs);

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["ServiceId"] = service.Id,
            ["ServiceName"] = service.Name,
            ["HealthStatus"] = status.ToString(),
            ["HttpStatus"] = probe.HttpStatusCode ?? 0,
            ["DurationMs"] = probe.ResponseTimeMs
        }))
        {
            logger.LogInformation(
                "Health check completed for Service {ServiceId} with HealthStatus {HealthStatus} HTTP {HttpStatusCode} after {DurationMs} ms",
                service.Id,
                status,
                probe.HttpStatusCode,
                probe.ResponseTimeMs);
        }
    }

    private async Task ApplyIncidentAsync(
        MonitoredService service,
        ServiceStatus status,
        HealthProbeResult probe,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (status is not ServiceStatus.Unhealthy and not ServiceStatus.Offline)
        {
            return;
        }

        var openIncident = await incidents.GetOpenForServiceAsync(service.Id, cancellationToken);
        var severity = status == ServiceStatus.Offline ? IncidentSeverity.Critical : IncidentSeverity.High;
        var title = $"{service.Name} is {status}";
        var description =
            $"Health checks reported {status}. {probe.Message} Consecutive failures: {service.ConsecutiveFailureCount}.";

        if (openIncident is null)
        {
            var created = new Incident
            {
                Id = Guid.CreateVersion7(),
                ServiceId = service.Id,
                Title = title,
                Description = description,
                Severity = severity,
                Status = IncidentStatus.Open,
                DetectedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            await incidents.AddAsync(created, cancellationToken);

            logger.LogInformation(
                "Created incident {IncidentId} for Service {ServiceId} after health status became {HealthStatus}",
                created.Id,
                service.Id,
                status);
            metrics.RecordIncidentCreated(service.Id, created.Id, severity);
            return;
        }

        openIncident.Title = title;
        openIncident.Description = description;
        if (severity > openIncident.Severity)
        {
            openIncident.Severity = severity;
        }

        openIncident.UpdatedAt = now;

        logger.LogInformation(
            "Updated open incident {IncidentId} for Service {ServiceId} instead of creating a duplicate",
            openIncident.Id,
            service.Id);
        metrics.RecordIncidentUpdated(service.Id, openIncident.Id, openIncident.Severity);
    }

    private async Task ObserveContainerAsync(
        MonitoredService service,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(service.ContainerName))
        {
            return;
        }

        var hadObservation = service.ContainerObservedAt.HasValue;
        var previouslyRunning = service.ContainerRunning == true;
        var previousRestartCount = service.ContainerRestartCount;

        ContainerSnapshot? snapshot;
        try
        {
            snapshot = await containers.GetByNameAsync(service.ContainerName, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Container inspection failed for Service {ServiceId}", service.Id);
            return;
        }

        if (snapshot is null)
        {
            return;
        }

        service.ContainerId = snapshot.Id;
        service.ContainerImage = snapshot.Image;
        service.ContainerRunning = snapshot.Found && snapshot.Running;
        service.ContainerStartedAt = snapshot.StartedAt;
        service.ContainerRestartCount = snapshot.RestartCount;
        service.ContainerHealth = snapshot.Found ? snapshot.Health : ContainerHealth.NotFound;
        service.ContainerCpuPercent = snapshot.CpuPercent;
        service.ContainerMemoryBytes = snapshot.MemoryUsageBytes;
        service.ContainerObservedAt = now;
        service.UpdatedAt = now;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["ServiceId"] = service.Id,
            ["ContainerName"] = service.ContainerName,
            ["ContainerRunning"] = snapshot.Running,
            ["RestartCount"] = snapshot.RestartCount
        }))
        {
            logger.LogInformation(
                "Container observation for Service {ServiceId} name {ContainerName} running {ContainerRunning} restarts {RestartCount} health {ContainerHealth}",
                service.Id,
                service.ContainerName,
                snapshot.Found && snapshot.Running,
                snapshot.RestartCount,
                service.ContainerHealth);
        }

        var kind = ContainerIncidentEvaluator.Evaluate(
            hadObservation,
            previouslyRunning,
            previousRestartCount,
            snapshot,
            options.Value.ContainerRestartIncidentThreshold);

        if (kind is null)
        {
            return;
        }

        var (severity, title, description) = kind switch
        {
            ContainerIncidentKind.Stopped => (
                IncidentSeverity.Critical,
                $"{service.Name} container stopped",
                $"Container '{service.ContainerName}' is not running. This is not an expected lifecycle event while monitoring is enabled."),
            ContainerIncidentKind.RepeatedRestarts => (
                IncidentSeverity.High,
                $"{service.Name} container is restarting",
                $"Container '{service.ContainerName}' restart count is {snapshot.RestartCount} (was {previousRestartCount})."),
            ContainerIncidentKind.Unhealthy => (
                IncidentSeverity.High,
                $"{service.Name} container health is unhealthy",
                $"Docker health check for '{service.ContainerName}' is unhealthy."),
            _ => (
                IncidentSeverity.High,
                $"{service.Name} container issue",
                $"Container '{service.ContainerName}' reported an operational problem.")
        };

        await ApplyNamedIncidentAsync(service, severity, title, description, now, cancellationToken);
    }

    private async Task ApplyNamedIncidentAsync(
        MonitoredService service,
        IncidentSeverity severity,
        string title,
        string description,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var openIncident = await incidents.GetOpenForServiceAsync(service.Id, cancellationToken);
        if (openIncident is null)
        {
            var created = new Incident
            {
                Id = Guid.CreateVersion7(),
                ServiceId = service.Id,
                Title = title,
                Description = description,
                Severity = severity,
                Status = IncidentStatus.Open,
                DetectedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            await incidents.AddAsync(created, cancellationToken);
            logger.LogInformation(
                "Created incident {IncidentId} for Service {ServiceId} from container monitoring",
                created.Id,
                service.Id);
            metrics.RecordIncidentCreated(service.Id, created.Id, severity);
            return;
        }

        openIncident.Title = title;
        openIncident.Description = description;
        if (severity > openIncident.Severity)
        {
            openIncident.Severity = severity;
        }

        openIncident.UpdatedAt = now;
        logger.LogInformation(
            "Updated open incident {IncidentId} for Service {ServiceId} from container monitoring",
            openIncident.Id,
            service.Id);
        metrics.RecordIncidentUpdated(service.Id, openIncident.Id, openIncident.Severity);
    }
}
