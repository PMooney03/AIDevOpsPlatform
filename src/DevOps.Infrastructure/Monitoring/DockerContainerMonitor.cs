using Docker.DotNet;
using Docker.DotNet.Models;
using DevOps.Application.Abstractions;
using DevOps.Application.Monitoring;
using DevOps.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOps.Infrastructure.Monitoring;

public sealed class DockerContainerMonitor : IContainerMonitor, IDisposable
{
    private readonly DockerOptions _options;
    private readonly ILogger<DockerContainerMonitor> _logger;
    private readonly Lazy<DockerClient?> _client;

    public DockerContainerMonitor(IOptions<DockerOptions> options, ILogger<DockerContainerMonitor> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new Lazy<DockerClient?>(CreateClient);
    }

    public async Task<ContainerSnapshot?> GetByNameAsync(string containerName, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var client = _client.Value;
        if (client is null)
        {
            return null;
        }

        try
        {
            var inspect = await client.Containers.InspectContainerAsync(containerName, cancellationToken);
            var stats = await TryReadStatsAsync(client, inspect.ID, cancellationToken);
            return new ContainerSnapshot(
                Found: true,
                Name: TrimName(inspect.Name) ?? containerName,
                Id: inspect.ID,
                Image: inspect.Config?.Image,
                Running: inspect.State?.Running == true,
                StartedAt: ParseTimestamp(inspect.State?.StartedAt),
                RestartCount: inspect.RestartCount,
                Health: MapHealth(inspect.State?.Health?.Status),
                CpuPercent: stats.CpuPercent,
                MemoryUsageBytes: stats.MemoryBytes);
        }
        catch (DockerContainerNotFoundException)
        {
            return new ContainerSnapshot(
                false,
                containerName,
                null,
                null,
                false,
                null,
                0,
                ContainerHealth.NotFound,
                null,
                null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Docker inspect failed for container {ContainerName}", containerName);
            return null;
        }
    }

    public async Task<ContainerOperationResult> RestartByNameAsync(string containerName, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return new ContainerOperationResult(false, "Docker monitoring is disabled.");
        }

        var client = _client.Value;
        if (client is null)
        {
            return new ContainerOperationResult(false, "The Docker client is unavailable.");
        }

        try
        {
            await client.Containers.RestartContainerAsync(
                containerName,
                new ContainerRestartParameters(),
                cancellationToken);
            return new ContainerOperationResult(true, $"Container '{containerName}' restart requested.");
        }
        catch (DockerContainerNotFoundException)
        {
            return new ContainerOperationResult(false, $"Container '{containerName}' was not found.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Docker restart failed for container {ContainerName}", containerName);
            return new ContainerOperationResult(false, "Docker restart failed.");
        }
    }

    public async Task<string> GetLogsAsync(string containerName, int tail, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return "Docker monitoring is disabled; logs were not collected.";
        }

        var client = _client.Value;
        if (client is null)
        {
            return "The Docker client is unavailable.";
        }

        try
        {
            var multiplexed = await client.Containers.GetContainerLogsAsync(
                containerName,
                tty: false,
                new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Tail = Math.Clamp(tail, 1, 500).ToString()
                },
                cancellationToken);
            var (stdout, stderr) = await multiplexed.ReadOutputToEndAsync(cancellationToken);
            var text = $"{stdout}{stderr}";
            return string.IsNullOrWhiteSpace(text) ? "(no log output)" : text;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Docker logs failed for container {ContainerName}", containerName);
            return "Docker logs could not be collected.";
        }
    }

    public void Dispose()
    {
        if (_client.IsValueCreated)
        {
            _client.Value?.Dispose();
        }
    }

    private DockerClient? CreateClient()
    {
        try
        {
            var endpoint = string.IsNullOrWhiteSpace(_options.Endpoint)
                ? "unix:///var/run/docker.sock"
                : _options.Endpoint;
            return new DockerClientConfiguration(new Uri(endpoint)).CreateClient();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Docker client could not be created");
            return null;
        }
    }

    private static async Task<(double? CpuPercent, long? MemoryBytes)> TryReadStatsAsync(
        DockerClient client,
        string containerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var completion = new TaskCompletionSource<ContainerStatsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));

            var progress = new Progress<ContainerStatsResponse>(stats =>
            {
                completion.TrySetResult(stats);
            });

            await client.Containers.GetContainerStatsAsync(
                containerId,
                new ContainerStatsParameters { Stream = false },
                progress,
                timeout.Token);

            var stats = await completion.Task.WaitAsync(timeout.Token);
            return (CalculateCpuPercent(stats), (long?)stats.MemoryStats?.Usage);
        }
        catch
        {
            return (null, null);
        }
    }

    private static double? CalculateCpuPercent(ContainerStatsResponse stats)
    {
        if (stats.CPUStats?.CPUUsage is null || stats.PreCPUStats?.CPUUsage is null)
        {
            return null;
        }

        var cpuDelta = (double)stats.CPUStats.CPUUsage.TotalUsage - stats.PreCPUStats.CPUUsage.TotalUsage;
        var systemDelta = (double)stats.CPUStats.SystemUsage - stats.PreCPUStats.SystemUsage;
        if (cpuDelta < 0 || systemDelta <= 0)
        {
            return null;
        }

        var cpuCount = stats.CPUStats.OnlineCPUs != 0
            ? stats.CPUStats.OnlineCPUs
            : (uint)(stats.CPUStats.CPUUsage.PercpuUsage?.Count ?? 1);
        if (cpuCount == 0)
        {
            cpuCount = 1;
        }

        return cpuDelta / systemDelta * cpuCount * 100.0;
    }

    private static ContainerHealth MapHealth(string? status) =>
        status?.ToLowerInvariant() switch
        {
            "healthy" => ContainerHealth.Healthy,
            "unhealthy" => ContainerHealth.Unhealthy,
            "starting" => ContainerHealth.Starting,
            null or "" => ContainerHealth.None,
            _ => ContainerHealth.Unknown
        };

    private static string? TrimName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? name : name.TrimStart('/');

    private static DateTimeOffset? ParseTimestamp(string? value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
}
