using DevOps.Application.Contracts;
using DevOps.Core.Entities;
using DevOps.Core.Enums;

namespace DevOps.Application.Mapping;

internal static class ContractMapping
{
    public static MonitoredServiceResponse ToResponse(this MonitoredService service) =>
        new(
            service.Id,
            service.Name,
            service.Description,
            service.BaseUrl,
            service.HealthEndpoint,
            service.Status,
            service.LastHealthCheckAt,
            service.LastHealthyAt,
            service.CreatedAt,
            service.UpdatedAt,
            service.MonitoringEnabled,
            service.ContainerName);

    public static ContainerRuntimeResponse ToContainerResponse(this MonitoredService service) =>
        new(
            service.ContainerName ?? string.Empty,
            service.ContainerId,
            service.ContainerImage,
            service.ContainerHealth != ContainerHealth.NotFound && service.ContainerObservedAt is not null,
            service.ContainerRunning ?? false,
            service.ContainerStartedAt,
            service.ContainerRestartCount,
            service.ContainerHealth,
            service.ContainerCpuPercent,
            service.ContainerMemoryBytes,
            service.ContainerObservedAt);

    public static IncidentResponse ToResponse(this Incident incident) =>
        new(
            incident.Id,
            incident.ServiceId,
            incident.Title,
            incident.Description,
            incident.Severity,
            incident.Status,
            incident.DetectedAt,
            incident.ResolvedAt,
            incident.Resolution,
            incident.RootCause,
            incident.ActionsTaken,
            incident.CreatedAt,
            incident.UpdatedAt);

    public static HealthCheckResultResponse ToResponse(this HealthCheckResult result) =>
        new(
            result.Id,
            result.ServiceId,
            result.Status,
            result.ResponseTimeMs,
            result.HttpStatusCode,
            result.Message,
            result.CheckedAt);
}
