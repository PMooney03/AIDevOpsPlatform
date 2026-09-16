using DevOps.Core.Enums;

namespace DevOps.Application.Monitoring;

public static class HealthStatusEvaluator
{
    public static ServiceStatus Evaluate(
        HealthProbeResult probe,
        int consecutiveFailureCount,
        DateTimeOffset? unreachableSince,
        MonitoringOptions options,
        DateTimeOffset now)
    {
        if (!probe.ReachedHost)
        {
            if (unreachableSince is not null &&
                now - unreachableSince.Value >= options.OfflineUnreachablePeriod)
            {
                return ServiceStatus.Offline;
            }

            return consecutiveFailureCount >= options.UnhealthyFailureThreshold
                ? ServiceStatus.Unhealthy
                : ServiceStatus.Degraded;
        }

        if (!probe.IsSuccessStatusCode)
        {
            return consecutiveFailureCount >= options.UnhealthyFailureThreshold
                ? ServiceStatus.Unhealthy
                : ServiceStatus.Degraded;
        }

        return probe.ResponseTimeMs >= options.DegradedResponseTimeMs
            ? ServiceStatus.Degraded
            : ServiceStatus.Healthy;
    }
}
