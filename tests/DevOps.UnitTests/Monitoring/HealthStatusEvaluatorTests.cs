using DevOps.Application.Monitoring;
using DevOps.Core.Enums;

namespace DevOps.UnitTests.Monitoring;

public class HealthStatusEvaluatorTests
{
    private static readonly MonitoringOptions Options = new()
    {
        DegradedResponseTimeMs = 1000,
        UnhealthyFailureThreshold = 3,
        OfflineUnreachablePeriod = TimeSpan.FromMinutes(2)
    };

    [Fact]
    public void Successful_fast_check_is_healthy()
    {
        var status = HealthStatusEvaluator.Evaluate(
            new HealthProbeResult(true, true, 200, 40, "ok"),
            consecutiveFailureCount: 0,
            unreachableSince: null,
            Options,
            DateTimeOffset.UtcNow);

        Assert.Equal(ServiceStatus.Healthy, status);
    }

    [Fact]
    public void Slow_success_is_degraded()
    {
        var status = HealthStatusEvaluator.Evaluate(
            new HealthProbeResult(true, true, 200, 1500, "slow"),
            0,
            null,
            Options,
            DateTimeOffset.UtcNow);

        Assert.Equal(ServiceStatus.Degraded, status);
    }

    [Fact]
    public void Failed_check_below_threshold_is_degraded()
    {
        var status = HealthStatusEvaluator.Evaluate(
            new HealthProbeResult(true, false, 500, 20, "error"),
            consecutiveFailureCount: 1,
            unreachableSince: null,
            Options,
            DateTimeOffset.UtcNow);

        Assert.Equal(ServiceStatus.Degraded, status);
    }

    [Fact]
    public void Consecutive_failures_become_unhealthy()
    {
        var status = HealthStatusEvaluator.Evaluate(
            new HealthProbeResult(true, false, 500, 20, "error"),
            consecutiveFailureCount: 3,
            unreachableSince: null,
            Options,
            DateTimeOffset.UtcNow);

        Assert.Equal(ServiceStatus.Unhealthy, status);
    }

    [Fact]
    public void Unreachable_within_period_is_not_offline()
    {
        var now = DateTimeOffset.UtcNow;
        var status = HealthStatusEvaluator.Evaluate(
            new HealthProbeResult(false, false, null, 5, "timeout"),
            consecutiveFailureCount: 1,
            unreachableSince: now.AddSeconds(-30),
            Options,
            now);

        Assert.Equal(ServiceStatus.Degraded, status);
    }

    [Fact]
    public void Unreachable_for_configured_period_is_offline()
    {
        var now = DateTimeOffset.UtcNow;
        var status = HealthStatusEvaluator.Evaluate(
            new HealthProbeResult(false, false, null, 5, "timeout"),
            consecutiveFailureCount: 1,
            unreachableSince: now.AddMinutes(-3),
            Options,
            now);

        Assert.Equal(ServiceStatus.Offline, status);
    }
}
