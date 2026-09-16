using DevOps.Core.Entities;
using DevOps.Core.Enums;

namespace DevOps.Application.Abstractions;

public interface IPlatformMetrics
{
    void RecordHealthCheck(
        string serviceName,
        Guid serviceId,
        ServiceStatus status,
        HealthCheckOutcome outcome,
        int? httpStatusCode,
        long durationMs);

    void RecordIncidentCreated(Guid serviceId, Guid incidentId, IncidentSeverity severity);

    void RecordIncidentUpdated(Guid serviceId, Guid incidentId, IncidentSeverity severity);

    void SetInventorySnapshot(IReadOnlyList<MonitoredService> services, IReadOnlyList<Incident> incidents);
}

public enum HealthCheckOutcome
{
    Success = 0,
    Failure = 1
}
