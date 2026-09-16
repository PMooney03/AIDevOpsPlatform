using DevOps.Core.Enums;

namespace DevOps.Application.Monitoring;

public sealed record ContainerSnapshot(
    bool Found,
    string Name,
    string? Id,
    string? Image,
    bool Running,
    DateTimeOffset? StartedAt,
    long RestartCount,
    ContainerHealth Health,
    double? CpuPercent,
    long? MemoryUsageBytes);
