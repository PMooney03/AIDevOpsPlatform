using DevOps.Core.Entities;

namespace DevOps.Application.Abstractions;

public interface IAiAnalysisRepository
{
    Task AddAsync(AiAnalysis analysis, CancellationToken cancellationToken);
    Task<AiAnalysis?> GetLatestForIncidentAsync(Guid incidentId, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IDeploymentRepository
{
    Task AddAsync(Deployment deployment, CancellationToken cancellationToken);
    Task<IReadOnlyList<Deployment>> GetForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<Deployment?> GetLatestForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
