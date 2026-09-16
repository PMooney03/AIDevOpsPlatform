using DevOps.Core.Entities;

namespace DevOps.Application.Abstractions;

public interface IMonitoredServiceRepository
{
    Task<IReadOnlyList<MonitoredService>> GetAllAsync(CancellationToken cancellationToken);
    Task<MonitoredService?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<MonitoredService?> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetEnabledIdsAsync(CancellationToken cancellationToken);
    Task AddAsync(MonitoredService service, CancellationToken cancellationToken);
    void Remove(MonitoredService service);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
