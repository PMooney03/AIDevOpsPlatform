using DevOps.Application.Analysis;

namespace DevOps.UnitTests.Analysis;

public class IncidentAnalysisValidatorTests
{
    [Fact]
    public void Accepts_complete_json()
    {
        const string json = """
            {
              "summary": "Database timeouts after deploy.",
              "probableCause": "PostgreSQL connectivity failure.",
              "severityAssessment": "High",
              "evidence": ["HTTP 500 increased"],
              "recommendedChecks": ["Verify PostgreSQL health"],
              "suggestedRemediation": ["Check DB_HOST"],
              "limitations": ["No query logs supplied"]
            }
            """;

        Assert.True(IncidentAnalysisValidator.TryParse(json, out var result, out _));
        Assert.True(result.Succeeded);
        Assert.Equal("PostgreSQL connectivity failure.", result.ProbableCause);
        Assert.Contains("HTTP 500 increased", result.Evidence);
    }

    [Fact]
    public void Rejects_missing_probable_cause()
    {
        Assert.False(IncidentAnalysisValidator.TryParse("""{"summary":"x"}""", out _, out var error));
        Assert.Contains("probableCause", error, StringComparison.OrdinalIgnoreCase);
    }
}
