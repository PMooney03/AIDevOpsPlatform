using DevOps.Application.Abstractions;
using DevOps.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevOps.Infrastructure.Persistence.Repositories;

public sealed class MonitoredServiceRepository(ApplicationDbContext dbContext) : IMonitoredServiceRepository
{
    public async Task<IReadOnlyList<MonitoredService>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.MonitoredServices
            .AsNoTracking()
            .OrderBy(service => service.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<MonitoredService?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.MonitoredServices
            .Include(service => service.Incidents)
            .FirstOrDefaultAsync(service => service.Id == id, cancellationToken);
    }

    public Task<MonitoredService?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        return dbContext.MonitoredServices
            .FirstOrDefaultAsync(service => service.Name == name, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.MonitoredServices.AnyAsync(service => service.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetEnabledIdsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.MonitoredServices
            .AsNoTracking()
            .Where(service => service.MonitoringEnabled)
            .Select(service => service.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MonitoredService service, CancellationToken cancellationToken)
    {
        await dbContext.MonitoredServices.AddAsync(service, cancellationToken);
    }

    public void Remove(MonitoredService service)
    {
        dbContext.MonitoredServices.Remove(service);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
