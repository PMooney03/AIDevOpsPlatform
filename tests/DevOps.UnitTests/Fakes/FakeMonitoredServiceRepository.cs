using DevOps.Application.Abstractions;
using DevOps.Core.Entities;

namespace DevOps.UnitTests.Fakes;

internal sealed class FakeMonitoredServiceRepository : IMonitoredServiceRepository
{
    private readonly List<MonitoredService> _services = [];

    public Task<IReadOnlyList<MonitoredService>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<MonitoredService>>(_services.OrderBy(s => s.Name).ToArray());
    }

    public Task<MonitoredService?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_services.FirstOrDefault(service => service.Id == id));
    }

    public Task<MonitoredService?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        return Task.FromResult(_services.FirstOrDefault(service => service.Name == name));
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_services.Any(service => service.Id == id));
    }

    public Task<IReadOnlyList<Guid>> GetEnabledIdsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(
            _services.Where(service => service.MonitoringEnabled).Select(service => service.Id).ToArray());
    }

    public Task AddAsync(MonitoredService service, CancellationToken cancellationToken)
    {
        _services.Add(service);
        return Task.CompletedTask;
    }

    public void Remove(MonitoredService service)
    {
        _services.Remove(service);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }
}
