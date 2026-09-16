using DevOps.Core.Enums;

namespace DevOps.Application.Contracts;

public sealed record CreateIncidentRequest(
    Guid ServiceId,
    string Title,
    string? Description,
    IncidentSeverity Severity,
    DateTimeOffset? DetectedAt = null);

public sealed record UpdateIncidentRequest(
    string Title,
    string? Description,
    IncidentSeverity Severity,
    IncidentStatus Status,
    string? Resolution);

public sealed record IncidentResponse(
    Guid Id,
    Guid ServiceId,
    string Title,
    string? Description,
    IncidentSeverity Severity,
    IncidentStatus Status,
    DateTimeOffset DetectedAt,
    DateTimeOffset? ResolvedAt,
    string? Resolution,
    string? RootCause,
    string? ActionsTaken,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
