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

    [Fact]
    public void Accepts_evidence_as_a_string()
    {
        const string json = """
            {
              "summary": "Health endpoint is failing.",
              "probableCause": "payments-api returns HTTP 500.",
              "evidence": "Health endpoint returned HTTP 500 Internal Server Error."
            }
            """;

        Assert.True(IncidentAnalysisValidator.TryParse(json, out var result, out _));
        Assert.Equal("Health endpoint returned HTTP 500 Internal Server Error.", Assert.Single(result.Evidence));
    }

    [Fact]
    public void Accepts_snake_case_keys_and_fenced_json()
    {
        const string json = """
            ```json
            {
              "summary": "Unhealthy demo service.",
              "probable_cause": "nginx returns 500 on /health.",
              "recommended_checks": "Inspect demo-unhealthy logs."
            }
            ```
            """;

        Assert.True(IncidentAnalysisValidator.TryParse(json, out var result, out _));
        Assert.Equal("nginx returns 500 on /health.", result.ProbableCause);
        Assert.Equal("Inspect demo-unhealthy logs.", Assert.Single(result.RecommendedChecks));
    }

    [Fact]
    public void Accepts_evidence_objects_inside_an_array()
    {
        const string json = """
            {
              "summary": "Consecutive health failures.",
              "probableCause": "Application health check is failing.",
              "evidence": [{ "http": 500, "status": "Unhealthy" }]
            }
            """;

        Assert.True(IncidentAnalysisValidator.TryParse(json, out var result, out _));
        Assert.Contains("http: 500", Assert.Single(result.Evidence));
    }
}
