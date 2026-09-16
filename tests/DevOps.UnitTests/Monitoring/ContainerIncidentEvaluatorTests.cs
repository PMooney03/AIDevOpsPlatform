using DevOps.Application.Monitoring;
using DevOps.Core.Enums;

namespace DevOps.UnitTests.Monitoring;

public class ContainerIncidentEvaluatorTests
{
    [Fact]
    public void First_observation_of_a_missing_container_does_not_open_an_incident()
    {
        var kind = ContainerIncidentEvaluator.Evaluate(
            false,
            false,
            0,
            Missing(),
            3);

        Assert.Null(kind);
    }

    [Fact]
    public void Previously_running_container_that_disappears_is_stopped()
    {
        var kind = ContainerIncidentEvaluator.Evaluate(
            true,
            true,
            0,
            Missing(),
            3);

        Assert.Equal(ContainerIncidentKind.Stopped, kind);
    }

    [Fact]
    public void Previously_running_container_that_exits_is_stopped()
    {
        var kind = ContainerIncidentEvaluator.Evaluate(
            true,
            true,
            1,
            Running(false, 1, ContainerHealth.None),
            3);

        Assert.Equal(ContainerIncidentKind.Stopped, kind);
    }

    [Fact]
    public void Restart_count_crossing_threshold_opens_restart_incident()
    {
        var kind = ContainerIncidentEvaluator.Evaluate(
            true,
            true,
            2,
            Running(true, 3, ContainerHealth.None),
            3);

        Assert.Equal(ContainerIncidentKind.RepeatedRestarts, kind);
    }

    [Fact]
    public void Docker_unhealthy_opens_incident()
    {
        var kind = ContainerIncidentEvaluator.Evaluate(
            true,
            true,
            0,
            Running(true, 0, ContainerHealth.Unhealthy),
            3);

        Assert.Equal(ContainerIncidentKind.Unhealthy, kind);
    }

    [Fact]
    public void Null_snapshot_means_docker_unavailable()
    {
        Assert.Null(ContainerIncidentEvaluator.Evaluate(true, true, 0, null, 3));
    }

    private static ContainerSnapshot Missing() =>
        new(false, "demo", null, null, false, null, 0, ContainerHealth.NotFound, null, null);

    private static ContainerSnapshot Running(bool running, long restarts, ContainerHealth health) =>
        new(true, "demo", "abc", "alpine", running, DateTimeOffset.UtcNow, restarts, health, 1.2, 1024);
}
