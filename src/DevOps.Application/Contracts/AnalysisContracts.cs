using DevOps.Core.Enums;

namespace DevOps.Application.Contracts;

public sealed record IncidentAnalysisContext(
    Guid IncidentId,
    string IncidentTitle,
    string? IncidentDescription,
    IncidentSeverity Severity,
    DateTimeOffset DetectedAt,
    string ServiceName,
    ServiceStatus ServiceStatus,
    string? ContainerName,
    bool? ContainerRunning,
    long? ContainerRestartCount,
    ContainerHealth? ContainerHealth,
    IReadOnlyList<string> RecentHealthMessages,
    IReadOnlyList<SimilarIncidentExample> HistoricalExamples,
    string? LatestDeploymentSummary,
    string? DeploymentCorrelation);

public sealed record SimilarIncidentExample(
    Guid IncidentId,
    string ServiceName,
    DateTimeOffset DetectedAt,
    double Similarity,
    string? RootCause,
    string? Resolution);

public sealed record IncidentAnalysisModelResult(
    bool Succeeded,
    string? ModelName,
    string Summary,
    string ProbableCause,
    string SeverityAssessment,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> RecommendedChecks,
    IReadOnlyList<string> SuggestedRemediation,
    IReadOnlyList<string> Limitations,
    string? FailureReason);

public sealed record IncidentAnalysisResponse(
    Guid Id,
    Guid IncidentId,
    bool Succeeded,
    string? ModelName,
    string Summary,
    string ProbableCause,
    string SeverityAssessment,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> RecommendedChecks,
    IReadOnlyList<string> SuggestedRemediation,
    IReadOnlyList<string> Limitations,
    string? FailureReason,
    DateTimeOffset CreatedAt);

public sealed record ResolveIncidentRequest(
    string Resolution,
    string? RootCause,
    string? ActionsTaken);

public sealed record SimilarIncidentResponse(
    Guid IncidentId,
    Guid ServiceId,
    string ServiceName,
    DateTimeOffset DetectedAt,
    double Similarity,
    string? RootCause,
    string? Resolution);

public sealed record CreateDeploymentRequest(
    Guid ServiceId,
    string CommitSha,
    string Branch,
    DeploymentStageStatus BuildStatus,
    DeploymentStageStatus TestStatus,
    DeploymentStageStatus DeploymentStatus,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);

public sealed record DeploymentResponse(
    Guid Id,
    Guid ServiceId,
    string CommitSha,
    string Branch,
    DeploymentStageStatus BuildStatus,
    DeploymentStageStatus TestStatus,
    DeploymentStageStatus DeploymentStatus,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt);
