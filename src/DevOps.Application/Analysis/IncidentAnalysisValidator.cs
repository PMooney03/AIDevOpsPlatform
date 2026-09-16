using System.Text.Json;
using DevOps.Application.Contracts;

namespace DevOps.Application.Analysis;

public static class IncidentAnalysisValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool TryParse(string? json, out IncidentAnalysisModelResult result, out string? error)
    {
        result = new IncidentAnalysisModelResult(false, null, "", "", "", [], [], [], [], "Malformed model output.");
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Model returned an empty body.";
            return false;
        }

        OllamaAnalysisPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OllamaAnalysisPayload>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            error = $"Model output was not valid JSON: {exception.Message}";
            return false;
        }

        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.Summary) ||
            string.IsNullOrWhiteSpace(payload.ProbableCause))
        {
            error = "Model JSON was missing summary or probableCause.";
            return false;
        }

        result = new IncidentAnalysisModelResult(
            true,
            null,
            payload.Summary.Trim(),
            payload.ProbableCause.Trim(),
            string.IsNullOrWhiteSpace(payload.SeverityAssessment) ? "Unknown" : payload.SeverityAssessment.Trim(),
            payload.Evidence ?? [],
            payload.RecommendedChecks ?? [],
            payload.SuggestedRemediation ?? [],
            payload.Limitations ?? [],
            null);
        return true;
    }

    private sealed class OllamaAnalysisPayload
    {
        public string? Summary { get; set; }
        public string? ProbableCause { get; set; }
        public string? SeverityAssessment { get; set; }
        public List<string>? Evidence { get; set; }
        public List<string>? RecommendedChecks { get; set; }
        public List<string>? SuggestedRemediation { get; set; }
        public List<string>? Limitations { get; set; }
    }
}
