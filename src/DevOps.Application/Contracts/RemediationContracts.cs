using DevOps.Core.Enums;

namespace DevOps.Application.Contracts;

public sealed record CreateRemediationRequest(
    RemediationActionType ActionType,
    string? Description);

public sealed record RejectRemediationRequest(string? Reason);

public sealed record RemediationResponse(
    Guid Id,
    Guid IncidentId,
    Guid ServiceId,
    RemediationActionType ActionType,
    string Description,
    RemediationStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? ExecutedAt,
    string? ApprovedBy,
    string? RejectedBy,
    string? RejectionReason,
    string? Result,
    string? FailureReason,
    DateTimeOffset CreatedAt);

public sealed record OverviewResponse(
    int TotalServices,
    int Healthy,
    int Degraded,
    int Unhealthy,
    int Offline,
    int Unknown,
    int OpenIncidents,
    int CriticalIncidents);

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string Token, string Username, string Role, DateTimeOffset ExpiresAt);
