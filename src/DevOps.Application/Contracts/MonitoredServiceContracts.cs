using DevOps.Core.Enums;

namespace DevOps.Application.Contracts;

public sealed record CreateMonitoredServiceRequest(
    string Name,
    string? Description,
    string BaseUrl,
    string HealthEndpoint,
    bool MonitoringEnabled = true,
    string? ContainerName = null);

public sealed record UpdateMonitoredServiceRequest(
    string Name,
    string? Description,
    string BaseUrl,
    string HealthEndpoint,
    bool MonitoringEnabled,
    string? ContainerName = null);

public sealed record MonitoredServiceResponse(
    Guid Id,
    string Name,
    string? Description,
    string BaseUrl,
    string HealthEndpoint,
    ServiceStatus Status,
    DateTimeOffset? LastHealthCheckAt,
    DateTimeOffset? LastHealthyAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool MonitoringEnabled,
    string? ContainerName);

public sealed record ContainerRuntimeResponse(
    string Name,
    string? Id,
    string? Image,
    bool Found,
    bool Running,
    DateTimeOffset? StartedAt,
    long RestartCount,
    ContainerHealth Health,
    double? CpuPercent,
    long? MemoryBytes,
    DateTimeOffset? ObservedAt);

public sealed record ServiceRuntimeResponse(
    Guid ServiceId,
    string ServiceName,
    ServiceStatus ApplicationStatus,
    HealthCheckResultResponse? LatestHealth,
    ContainerRuntimeResponse? Container);
