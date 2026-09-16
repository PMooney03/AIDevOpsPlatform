using DevOps.Core.Entities;

namespace DevOps.Application.Abstractions;

public interface IIncidentRepository
{
    Task<IReadOnlyList<Incident>> GetAllAsync(CancellationToken cancellationToken);
    Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasAnyForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<Incident?> GetOpenForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Incident>> GetResolvedAsync(CancellationToken cancellationToken);
    Task AddAsync(Incident incident, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
