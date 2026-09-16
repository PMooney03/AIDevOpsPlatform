using DevOps.Core.Entities;
using DevOps.Core.Enums;

namespace DevOps.UnitTests.Domain;

public class IncidentStatusTests
{
    [Fact]
    public void ApplyStatus_resolved_sets_resolved_at_once()
    {
        var incident = new Incident
        {
            Id = Guid.CreateVersion7(),
            ServiceId = Guid.CreateVersion7(),
            Title = "Database timeouts",
            Severity = IncidentSeverity.High,
            Status = IncidentStatus.Open,
            DetectedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var first = new DateTimeOffset(2026, 9, 16, 19, 42, 0, TimeSpan.Zero);
        incident.ApplyStatus(IncidentStatus.Resolved, first);

        Assert.Equal(IncidentStatus.Resolved, incident.Status);
        Assert.Equal(first, incident.ResolvedAt);

        incident.ApplyStatus(IncidentStatus.Resolved, first.AddMinutes(10));
        Assert.Equal(first, incident.ResolvedAt);
    }

    [Fact]
    public void ApplyStatus_reopened_clears_resolved_at()
    {
        var incident = new Incident
        {
            Id = Guid.CreateVersion7(),
            ServiceId = Guid.CreateVersion7(),
            Title = "Database timeouts",
            Severity = IncidentSeverity.High,
            DetectedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        incident.ApplyStatus(IncidentStatus.Resolved, DateTimeOffset.UtcNow);
        incident.ApplyStatus(IncidentStatus.Investigating, DateTimeOffset.UtcNow);

        Assert.Equal(IncidentStatus.Investigating, incident.Status);
        Assert.Null(incident.ResolvedAt);
    }

    [Theory]
    [InlineData(IncidentSeverity.Low)]
    [InlineData(IncidentSeverity.Medium)]
    [InlineData(IncidentSeverity.High)]
    [InlineData(IncidentSeverity.Critical)]
    public void Incident_can_use_all_severity_values(IncidentSeverity severity)
    {
        var incident = new Incident
        {
            Id = Guid.CreateVersion7(),
            ServiceId = Guid.CreateVersion7(),
            Title = "Test",
            Severity = severity,
            DetectedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        Assert.Equal(severity, incident.Severity);
        Assert.Equal(IncidentStatus.Open, incident.Status);
    }
}
