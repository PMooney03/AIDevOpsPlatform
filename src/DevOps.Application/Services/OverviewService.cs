using DevOps.Application.Abstractions;
using DevOps.Application.Contracts;
using DevOps.Core.Enums;

namespace DevOps.Application.Services;

public sealed class OverviewService(
    IMonitoredServiceRepository services,
    IIncidentRepository incidents)
{
    public async Task<OverviewResponse> GetAsync(CancellationToken cancellationToken)
    {
        var monitored = await services.GetAllAsync(cancellationToken);
        var allIncidents = await incidents.GetAllAsync(cancellationToken);
        var open = allIncidents.Where(incident => incident.Status != IncidentStatus.Resolved).ToArray();

        return new OverviewResponse(
            monitored.Count,
            monitored.Count(service => service.Status == ServiceStatus.Healthy),
            monitored.Count(service => service.Status == ServiceStatus.Degraded),
            monitored.Count(service => service.Status == ServiceStatus.Unhealthy),
            monitored.Count(service => service.Status == ServiceStatus.Offline),
            monitored.Count(service => service.Status == ServiceStatus.Unknown),
            open.Length,
            open.Count(incident => incident.Severity == IncidentSeverity.Critical));
    }
}
