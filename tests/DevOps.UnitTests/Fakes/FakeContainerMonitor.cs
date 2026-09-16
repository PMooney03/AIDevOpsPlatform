using DevOps.Application.Abstractions;
using DevOps.Application.Monitoring;

namespace DevOps.UnitTests.Fakes;

internal sealed class FakeContainerMonitor : IContainerMonitor
{
    public ContainerSnapshot? Next { get; set; }

    public Task<ContainerSnapshot?> GetByNameAsync(string containerName, CancellationToken cancellationToken)
    {
        return Task.FromResult(Next);
    }

    public bool RestartSucceeded { get; set; } = true;
    public string RestartMessage { get; set; } = "Container restarted.";
    public string Logs { get; set; } = "demo log line";
    public int RestartCalls { get; private set; }

    public Task<ContainerOperationResult> RestartByNameAsync(string containerName, CancellationToken cancellationToken)
    {
        RestartCalls++;
        return Task.FromResult(new ContainerOperationResult(RestartSucceeded, RestartMessage));
    }

    public Task<string> GetLogsAsync(string containerName, int tail, CancellationToken cancellationToken)
    {
        return Task.FromResult(Logs);
    }
}
