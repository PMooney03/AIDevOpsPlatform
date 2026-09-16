using DevOps.Application.Abstractions;
using DevOps.Application.Monitoring;

namespace DevOps.Application.Monitoring;

public sealed class DisabledContainerMonitor : IContainerMonitor
{
    public Task<ContainerSnapshot?> GetByNameAsync(string containerName, CancellationToken cancellationToken)
    {
        return Task.FromResult<ContainerSnapshot?>(null);
    }

    public Task<ContainerOperationResult> RestartByNameAsync(string containerName, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ContainerOperationResult(
            false,
            "Docker monitoring is disabled, so the container was not restarted."));
    }

    public Task<string> GetLogsAsync(string containerName, int tail, CancellationToken cancellationToken)
    {
        return Task.FromResult("Docker monitoring is disabled; logs were not collected.");
    }
}
