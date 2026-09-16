using DevOps.Application.Monitoring;

namespace DevOps.Application.Abstractions;

public interface IContainerMonitor
{
    Task<ContainerSnapshot?> GetByNameAsync(string containerName, CancellationToken cancellationToken);
    Task<ContainerOperationResult> RestartByNameAsync(string containerName, CancellationToken cancellationToken);
    Task<string> GetLogsAsync(string containerName, int tail, CancellationToken cancellationToken);
}

public sealed record ContainerOperationResult(bool Succeeded, string Message);
