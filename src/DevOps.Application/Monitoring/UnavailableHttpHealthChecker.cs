using DevOps.Application.Abstractions;

namespace DevOps.Application.Monitoring;

public sealed class UnavailableHttpHealthChecker : IHttpHealthChecker
{
    public Task<HealthProbeResult> CheckAsync(Uri healthUrl, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HealthProbeResult(
            false,
            false,
            null,
            0,
            "HTTP health checker is not configured."));
    }
}
