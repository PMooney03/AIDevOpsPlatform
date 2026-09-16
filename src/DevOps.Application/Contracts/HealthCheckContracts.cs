using DevOps.Core.Entities;
using DevOps.Core.Enums;

namespace DevOps.Application.Contracts;

public sealed record HealthCheckResultResponse(
    Guid Id,
    Guid ServiceId,
    ServiceStatus Status,
    long ResponseTimeMs,
    int? HttpStatusCode,
    string? Message,
    DateTimeOffset CheckedAt);
