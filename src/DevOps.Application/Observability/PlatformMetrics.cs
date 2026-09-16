using System.Diagnostics;
using System.Diagnostics.Metrics;
using DevOps.Application.Abstractions;
using DevOps.Core.Entities;
using DevOps.Core.Enums;

namespace DevOps.Application.Observability;

public sealed class PlatformMetrics : IPlatformMetrics
{
    public const string MeterName = "DevOps.Platform";

    private readonly Counter<long> _healthChecks;
    private readonly Counter<long> _healthChecksFailed;
    private readonly Counter<long> _incidents;
    private readonly Histogram<double> _responseTime;
    private long _monitored;
    private long _healthy;
    private long _unhealthy;
    private long _degraded;
    private long _offline;
    private long _openIncidents;
    private long _openLow;
    private long _openMedium;
    private long _openHigh;
    private long _openCritical;

    public PlatformMetrics()
    {
        var meter = new Meter(MeterName, "1.0.0");
        _healthChecks = meter.CreateCounter<long>(
            "devops_health_checks",
            description: "Health checks performed by the monitoring worker.");
        _healthChecksFailed = meter.CreateCounter<long>(
            "devops_health_checks_failed",
            description: "Health checks that did not return a success HTTP status.");
        _incidents = meter.CreateCounter<long>(
            "devops_incidents",
            description: "Incidents created or updated.");
        _responseTime = meter.CreateHistogram<double>(
            "devops_service_response_time",
            unit: "ms",
            description: "Health-check response time in milliseconds.");

        meter.CreateObservableGauge(
            "devops_services_monitored",
            () => Interlocked.Read(ref _monitored),
            description: "Registered services with monitoring enabled.");
        meter.CreateObservableGauge(
            "devops_services_healthy",
            () => Interlocked.Read(ref _healthy),
            description: "Services currently Healthy.");
        meter.CreateObservableGauge(
            "devops_services_unhealthy",
            () => Interlocked.Read(ref _unhealthy),
            description: "Services currently Unhealthy or Offline.");
        meter.CreateObservableGauge(
            "devops_services_degraded",
            () => Interlocked.Read(ref _degraded),
            description: "Services currently Degraded.");
        meter.CreateObservableGauge(
            "devops_services_offline",
            () => Interlocked.Read(ref _offline),
            description: "Services currently Offline.");
        meter.CreateObservableGauge(
            "devops_incidents_open",
            () => Interlocked.Read(ref _openIncidents),
            description: "Incidents that are Open or Investigating.");
        meter.CreateObservableGauge(
            "devops_incidents_open_by_severity",
            GetOpenIncidentsBySeverity,
            description: "Open or investigating incidents grouped by severity.");
    }

    public void RecordHealthCheck(
        string serviceName,
        Guid serviceId,
        ServiceStatus status,
        HealthCheckOutcome outcome,
        int? httpStatusCode,
        long durationMs)
    {
        TagList tags = new()
        {
            { "service_name", serviceName },
            { "health_status", status.ToString() },
            { "result", outcome == HealthCheckOutcome.Success ? "success" : "failure" },
            { "http_status", httpStatusCode?.ToString() ?? "none" }
        };

        _healthChecks.Add(1, tags);
        _responseTime.Record(durationMs, tags);
        if (outcome == HealthCheckOutcome.Failure)
        {
            _healthChecksFailed.Add(1, tags);
        }
    }

    public void RecordIncidentCreated(Guid serviceId, Guid incidentId, IncidentSeverity severity)
    {
        _incidents.Add(1,
            new KeyValuePair<string, object?>("action", "created"),
            new KeyValuePair<string, object?>("severity", severity.ToString()));
    }

    public void RecordIncidentUpdated(Guid serviceId, Guid incidentId, IncidentSeverity severity)
    {
        _incidents.Add(1,
            new KeyValuePair<string, object?>("action", "updated"),
            new KeyValuePair<string, object?>("severity", severity.ToString()));
    }

    public void SetInventorySnapshot(IReadOnlyList<MonitoredService> services, IReadOnlyList<Incident> incidents)
    {
        Interlocked.Exchange(ref _monitored, services.Count(service => service.MonitoringEnabled));
        Interlocked.Exchange(ref _healthy, services.Count(service => service.Status == ServiceStatus.Healthy));
        Interlocked.Exchange(ref _unhealthy, services.Count(service =>
            service.Status is ServiceStatus.Unhealthy or ServiceStatus.Offline));
        Interlocked.Exchange(ref _degraded, services.Count(service => service.Status == ServiceStatus.Degraded));
        Interlocked.Exchange(ref _offline, services.Count(service => service.Status == ServiceStatus.Offline));
        Interlocked.Exchange(ref _openIncidents, incidents.Count(incident => incident.Status != IncidentStatus.Resolved));
        Interlocked.Exchange(ref _openLow, CountOpen(incidents, IncidentSeverity.Low));
        Interlocked.Exchange(ref _openMedium, CountOpen(incidents, IncidentSeverity.Medium));
        Interlocked.Exchange(ref _openHigh, CountOpen(incidents, IncidentSeverity.High));
        Interlocked.Exchange(ref _openCritical, CountOpen(incidents, IncidentSeverity.Critical));
    }

    private IEnumerable<Measurement<long>> GetOpenIncidentsBySeverity()
    {
        yield return new Measurement<long>(Interlocked.Read(ref _openLow), new KeyValuePair<string, object?>("severity", "Low"));
        yield return new Measurement<long>(Interlocked.Read(ref _openMedium), new KeyValuePair<string, object?>("severity", "Medium"));
        yield return new Measurement<long>(Interlocked.Read(ref _openHigh), new KeyValuePair<string, object?>("severity", "High"));
        yield return new Measurement<long>(Interlocked.Read(ref _openCritical), new KeyValuePair<string, object?>("severity", "Critical"));
    }

    private static long CountOpen(IReadOnlyList<Incident> incidents, IncidentSeverity severity) =>
        incidents.Count(incident => incident.Status != IncidentStatus.Resolved && incident.Severity == severity);
}
