using DevOps.Application.Abstractions;
using DevOps.Core.Entities;
using DevOps.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevOps.Infrastructure.Persistence.Repositories;

public sealed class IncidentRepository(ApplicationDbContext dbContext) : IIncidentRepository
{
    public async Task<IReadOnlyList<Incident>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Incidents
            .AsNoTracking()
            .OrderByDescending(incident => incident.DetectedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Incidents.FirstOrDefaultAsync(incident => incident.Id == id, cancellationToken);
    }

    public Task<bool> HasAnyForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return dbContext.Incidents.AnyAsync(incident => incident.ServiceId == serviceId, cancellationToken);
    }

    public Task<Incident?> GetOpenForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return dbContext.Incidents
            .Where(incident =>
                incident.ServiceId == serviceId &&
                incident.Status != IncidentStatus.Resolved)
            .OrderByDescending(incident => incident.DetectedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Incident>> GetResolvedAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Incidents
            .AsNoTracking()
            .Where(incident => incident.Status == IncidentStatus.Resolved)
            .OrderByDescending(incident => incident.DetectedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Incident incident, CancellationToken cancellationToken)
    {
        await dbContext.Incidents.AddAsync(incident, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
