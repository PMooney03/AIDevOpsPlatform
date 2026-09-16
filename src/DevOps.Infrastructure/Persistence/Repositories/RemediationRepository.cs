using DevOps.Application.Abstractions;
using DevOps.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevOps.Infrastructure.Persistence.Repositories;

public sealed class RemediationRepository(ApplicationDbContext dbContext) : IRemediationRepository
{
    public async Task AddAsync(RemediationAction action, CancellationToken cancellationToken)
    {
        await dbContext.Remediations.AddAsync(action, cancellationToken);
    }

    public Task<RemediationAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Remediations.FirstOrDefaultAsync(action => action.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RemediationAction>> GetForIncidentAsync(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Remediations
            .AsNoTracking()
            .Where(action => action.IncidentId == incidentId)
            .OrderByDescending(action => action.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
