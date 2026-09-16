using DevOps.Application.Abstractions;
using DevOps.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevOps.Infrastructure.Persistence.Repositories;

public sealed class HealthCheckResultRepository(ApplicationDbContext dbContext) : IHealthCheckResultRepository
{
    public async Task AddAsync(HealthCheckResult result, CancellationToken cancellationToken)
    {
        await dbContext.HealthCheckResults.AddAsync(result, cancellationToken);
    }

    public Task<HealthCheckResult?> GetLatestAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return dbContext.HealthCheckResults
            .AsNoTracking()
            .Where(result => result.ServiceId == serviceId)
            .OrderByDescending(result => result.CheckedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HealthCheckResult>> GetHistoryAsync(
        Guid serviceId,
        int limit,
        CancellationToken cancellationToken)
    {
        return await dbContext.HealthCheckResults
            .AsNoTracking()
            .Where(result => result.ServiceId == serviceId)
            .OrderByDescending(result => result.CheckedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var stale = await dbContext.HealthCheckResults
            .Where(result => result.CheckedAt < cutoff)
            .ToListAsync(cancellationToken);
        if (stale.Count == 0)
        {
            return 0;
        }

        dbContext.HealthCheckResults.RemoveRange(stale);
        await dbContext.SaveChangesAsync(cancellationToken);
        return stale.Count;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
