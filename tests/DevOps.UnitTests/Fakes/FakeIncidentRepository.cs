using DevOps.Application.Abstractions;
using DevOps.Core.Entities;

namespace DevOps.UnitTests.Fakes;

internal sealed class FakeIncidentRepository : IIncidentRepository
{
    private readonly List<Incident> _incidents = [];

    public Task<IReadOnlyList<Incident>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Incident>>(
            _incidents.OrderByDescending(incident => incident.DetectedAt).ToArray());
    }

    public Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_incidents.FirstOrDefault(incident => incident.Id == id));
    }

    public Task<bool> HasAnyForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_incidents.Any(incident => incident.ServiceId == serviceId));
    }

    public Task<Incident?> GetOpenForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_incidents
            .Where(incident =>
                incident.ServiceId == serviceId &&
                incident.Status != DevOps.Core.Enums.IncidentStatus.Resolved)
            .OrderByDescending(incident => incident.DetectedAt)
            .FirstOrDefault());
    }

    public Task<IReadOnlyList<Incident>> GetResolvedAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Incident>>(
            _incidents.Where(incident => incident.Status == DevOps.Core.Enums.IncidentStatus.Resolved).ToArray());
    }

    public Task AddAsync(Incident incident, CancellationToken cancellationToken)
    {
        _incidents.Add(incident);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }
}
