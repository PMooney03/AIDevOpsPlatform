using DevOps.Core.Enums;

namespace DevOps.Core.Entities;

public class MonitoredService
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string BaseUrl { get; set; }
    public required string HealthEndpoint { get; set; }
    public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
    public DateTimeOffset? LastHealthCheckAt { get; set; }
    public DateTimeOffset? LastHealthyAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool MonitoringEnabled { get; set; } = true;
    public int ConsecutiveFailureCount { get; set; }
    public DateTimeOffset? UnreachableSince { get; set; }
    public string? ContainerName { get; set; }
    public string? ContainerId { get; set; }
    public string? ContainerImage { get; set; }
    public bool? ContainerRunning { get; set; }
    public DateTimeOffset? ContainerStartedAt { get; set; }
    public long ContainerRestartCount { get; set; }
    public ContainerHealth ContainerHealth { get; set; } = ContainerHealth.Unknown;
    public double? ContainerCpuPercent { get; set; }
    public long? ContainerMemoryBytes { get; set; }
    public DateTimeOffset? ContainerObservedAt { get; set; }

    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    public ICollection<HealthCheckResult> HealthChecks { get; set; } = new List<HealthCheckResult>();

    public bool HasOpenIncidents() =>
        Incidents.Any(incident => incident.Status != IncidentStatus.Resolved);
}
