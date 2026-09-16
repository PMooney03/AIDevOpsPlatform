using DevOps.Core.Enums;

namespace DevOps.Core.Entities;

public class AiAnalysis
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public Incident? Incident { get; set; }
    public bool Succeeded { get; set; }
    public string? ModelName { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string ProbableCause { get; set; } = string.Empty;
    public string SeverityAssessment { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "[]";
    public string RecommendedChecksJson { get; set; } = "[]";
    public string SuggestedRemediationJson { get; set; } = "[]";
    public string LimitationsJson { get; set; } = "[]";
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class Deployment
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public MonitoredService? Service { get; set; }
    public required string CommitSha { get; set; }
    public required string Branch { get; set; }
    public DeploymentStageStatus BuildStatus { get; set; }
    public DeploymentStageStatus TestStatus { get; set; }
    public DeploymentStageStatus DeploymentStatus { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
