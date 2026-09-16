using DevOps.Application.Monitoring;

namespace DevOps.Application.Abstractions;

public interface IHttpHealthChecker
{
    Task<HealthProbeResult> CheckAsync(Uri healthUrl, CancellationToken cancellationToken);
}
