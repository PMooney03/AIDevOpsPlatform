namespace DevOps.Application.Abstractions;

public interface IServiceHealthMonitor
{
    Task MonitorEnabledServicesAsync(CancellationToken cancellationToken);
    Task CheckServiceAsync(Guid serviceId, CancellationToken cancellationToken);
}
