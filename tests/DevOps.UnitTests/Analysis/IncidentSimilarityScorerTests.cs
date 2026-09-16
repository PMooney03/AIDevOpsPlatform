using DevOps.Application.Analysis;
using DevOps.Core.Entities;
using DevOps.Core.Enums;

namespace DevOps.UnitTests.Analysis;

public class IncidentSimilarityScorerTests
{
    [Fact]
    public void Scores_resolved_database_incidents_highly()
    {
        var current = Incident("payments-api database timeouts", "PostgreSQL connectivity failure");
        var historical = Incident(
            "database connection timeout",
            "Incorrect database hostname following deployment.",
            IncidentStatus.Resolved,
            "Incorrect DB_HOST",
            "DB_HOST configuration corrected.");
        var unrelated = Incident("disk full on cache node", "ephemeral volume exhausted", IncidentStatus.Resolved);

        var ranked = IncidentSimilarityScorer.Rank(current, [historical, unrelated], 0.05, 5);

        Assert.Contains(ranked, item => item.Incident.Id == historical.Id);
        Assert.DoesNotContain(ranked, item => item.Incident.Id == unrelated.Id);
    }

    [Fact]
    public void Does_not_include_unresolved_incidents()
    {
        var current = Incident("database timeout", "postgres");
        var open = Incident("database timeout", "postgres", IncidentStatus.Open);

        Assert.Empty(IncidentSimilarityScorer.Rank(current, [open], 0.01, 5));
    }

    private static Incident Incident(
        string title,
        string? description,
        IncidentStatus status = IncidentStatus.Open,
        string? rootCause = null,
        string? resolution = null) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ServiceId = Guid.CreateVersion7(),
            Title = title,
            Description = description,
            Status = status,
            RootCause = rootCause,
            Resolution = resolution,
            DetectedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
