namespace DevOps.Core.Enums;

public enum RemediationActionType
{
    RestartContainer = 0,
    RunHealthCheck = 1,
    RefreshContainerStatus = 2,
    CollectRecentLogs = 3
}

public enum RemediationStatus
{
    Recommended = 0,
    Approved = 1,
    Rejected = 2,
    Executing = 3,
    Completed = 4,
    Failed = 5
}
