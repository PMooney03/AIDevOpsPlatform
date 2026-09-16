using DevOps.Application.Abstractions;
using DevOps.Core.Entities;

namespace DevOps.UnitTests.Fakes;

internal sealed class FakeAiAnalysisRepository : IAiAnalysisRepository
{
    public List<AiAnalysis> Items { get; } = [];

    public Task AddAsync(AiAnalysis analysis, CancellationToken cancellationToken)
    {
        Items.Add(analysis);
        return Task.CompletedTask;
    }

    public Task<AiAnalysis?> GetLatestForIncidentAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Items
            .Where(item => item.IncidentId == incidentId)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault());
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
}

internal sealed class FakeDeploymentRepository : IDeploymentRepository
{
    public List<Deployment> Items { get; } = [];

    public Task AddAsync(Deployment deployment, CancellationToken cancellationToken)
    {
        Items.Add(deployment);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Deployment>> GetForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Deployment>>(
            Items.Where(item => item.ServiceId == serviceId).OrderByDescending(item => item.StartedAt).ToArray());
    }

    public Task<Deployment?> GetLatestForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Items
            .Where(item => item.ServiceId == serviceId)
            .OrderByDescending(item => item.StartedAt)
            .FirstOrDefault());
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
}
