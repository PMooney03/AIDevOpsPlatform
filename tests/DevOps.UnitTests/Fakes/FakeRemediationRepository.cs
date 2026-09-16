using DevOps.Application.Abstractions;
using DevOps.Core.Entities;

namespace DevOps.UnitTests.Fakes;

internal sealed class FakeRemediationRepository : IRemediationRepository
{
    public List<RemediationAction> Items { get; } = [];

    public Task AddAsync(RemediationAction action, CancellationToken cancellationToken)
    {
        Items.Add(action);
        return Task.CompletedTask;
    }

    public Task<RemediationAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Items.FirstOrDefault(item => item.Id == id));
    }

    public Task<IReadOnlyList<RemediationAction>> GetForIncidentAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<RemediationAction>>(
            Items.Where(item => item.IncidentId == incidentId).ToArray());
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
}
