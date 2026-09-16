using DevOps.Application.Abstractions;
using DevOps.Core.Entities;
using DevOps.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevOps.Infrastructure.Persistence.Repositories;

public sealed class AiAnalysisRepository(ApplicationDbContext dbContext) : IAiAnalysisRepository
{
    public async Task AddAsync(AiAnalysis analysis, CancellationToken cancellationToken)
    {
        await dbContext.AiAnalyses.AddAsync(analysis, cancellationToken);
    }

    public Task<AiAnalysis?> GetLatestForIncidentAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        return dbContext.AiAnalyses
            .Where(analysis => analysis.IncidentId == incidentId)
            .OrderByDescending(analysis => analysis.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

public sealed class DeploymentRepository(ApplicationDbContext dbContext) : IDeploymentRepository
{
    public async Task AddAsync(Deployment deployment, CancellationToken cancellationToken)
    {
        await dbContext.Deployments.AddAsync(deployment, cancellationToken);
    }

    public async Task<IReadOnlyList<Deployment>> GetForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return await dbContext.Deployments
            .AsNoTracking()
            .Where(deployment => deployment.ServiceId == serviceId)
            .OrderByDescending(deployment => deployment.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Deployment?> GetLatestForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return dbContext.Deployments
            .Where(deployment => deployment.ServiceId == serviceId)
            .OrderByDescending(deployment => deployment.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
