using DevOps.Core.Entities;

namespace DevOps.Application.Abstractions;

public interface IHealthCheckResultRepository
{
    Task AddAsync(HealthCheckResult result, CancellationToken cancellationToken);
    Task<HealthCheckResult?> GetLatestAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<HealthCheckResult>> GetHistoryAsync(Guid serviceId, int limit, CancellationToken cancellationToken);
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
