using DevOps.Application.Abstractions;
using DevOps.Core.Entities;
using DevOps.Core.Enums;

namespace DevOps.UnitTests.Fakes;

internal sealed class FakePlatformMetrics : IPlatformMetrics
{
    public int HealthChecks { get; private set; }
    public int FailedHealthChecks { get; private set; }
    public int IncidentsCreated { get; private set; }
    public int IncidentsUpdated { get; private set; }

    public void RecordHealthCheck(
        string serviceName,
        Guid serviceId,
        ServiceStatus status,
        HealthCheckOutcome outcome,
        int? httpStatusCode,
        long durationMs)
    {
        HealthChecks++;
        if (outcome == HealthCheckOutcome.Failure)
        {
            FailedHealthChecks++;
        }
    }

    public void RecordIncidentCreated(Guid serviceId, Guid incidentId, IncidentSeverity severity)
    {
        IncidentsCreated++;
    }

    public void RecordIncidentUpdated(Guid serviceId, Guid incidentId, IncidentSeverity severity)
    {
        IncidentsUpdated++;
    }

    public void SetInventorySnapshot(IReadOnlyList<MonitoredService> services, IReadOnlyList<Incident> incidents)
    {
    }
}
