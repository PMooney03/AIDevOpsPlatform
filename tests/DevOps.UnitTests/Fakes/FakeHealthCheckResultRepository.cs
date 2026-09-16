using DevOps.Application.Abstractions;
using DevOps.Core.Entities;

namespace DevOps.UnitTests.Fakes;

internal sealed class FakeHealthCheckResultRepository : IHealthCheckResultRepository
{
    public List<HealthCheckResult> Results { get; } = [];

    public Task AddAsync(HealthCheckResult result, CancellationToken cancellationToken)
    {
        Results.Add(result);
        return Task.CompletedTask;
    }

    public Task<HealthCheckResult?> GetLatestAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Results
            .Where(result => result.ServiceId == serviceId)
            .OrderByDescending(result => result.CheckedAt)
            .FirstOrDefault());
    }

    public Task<IReadOnlyList<HealthCheckResult>> GetHistoryAsync(
        Guid serviceId,
        int limit,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<HealthCheckResult>>(Results
            .Where(result => result.ServiceId == serviceId)
            .OrderByDescending(result => result.CheckedAt)
            .Take(limit)
            .ToArray());
    }

    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var removed = Results.RemoveAll(result => result.CheckedAt < cutoff);
        return Task.FromResult(removed);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Results.Count);
    }
}
