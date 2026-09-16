using DevOps.Core.Enums;

namespace DevOps.Application.Monitoring;

public enum ContainerIncidentKind
{
    Stopped = 0,
    RepeatedRestarts = 1,
    Unhealthy = 2
}

public static class ContainerIncidentEvaluator
{
    public static ContainerIncidentKind? Evaluate(
        bool hadPreviousObservation,
        bool previouslyRunning,
        long previousRestartCount,
        ContainerSnapshot? current,
        int restartIncidentThreshold)
    {
        if (current is null)
        {
            return null;
        }

        if (!current.Found)
        {
            return hadPreviousObservation && previouslyRunning
                ? ContainerIncidentKind.Stopped
                : null;
        }

        if (hadPreviousObservation && previouslyRunning && !current.Running)
        {
            return ContainerIncidentKind.Stopped;
        }

        if (current.Health == ContainerHealth.Unhealthy)
        {
            return ContainerIncidentKind.Unhealthy;
        }

        if (hadPreviousObservation &&
            current.RestartCount > previousRestartCount &&
            current.RestartCount >= restartIncidentThreshold)
        {
            return ContainerIncidentKind.RepeatedRestarts;
        }

        return null;
    }
}
