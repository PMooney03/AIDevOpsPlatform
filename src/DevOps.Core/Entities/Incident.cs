using DevOps.Core.Enums;

namespace DevOps.Core.Entities;

public class Incident
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public MonitoredService? Service { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public DateTimeOffset DetectedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
    public string? RootCause { get; set; }
    public string? ActionsTaken { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void ApplyStatus(IncidentStatus status, DateTimeOffset now)
    {
        Status = status;

        if (status == IncidentStatus.Resolved)
        {
            ResolvedAt ??= now;
            return;
        }

        ResolvedAt = null;
    }
}
