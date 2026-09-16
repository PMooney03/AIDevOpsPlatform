using DevOps.Core.Entities;
using DevOps.Core.Enums;

namespace DevOps.Application.Abstractions;

public interface IRemediationRepository
{
    Task AddAsync(RemediationAction action, CancellationToken cancellationToken);
    Task<RemediationAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<RemediationAction>> GetForIncidentAsync(Guid incidentId, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
