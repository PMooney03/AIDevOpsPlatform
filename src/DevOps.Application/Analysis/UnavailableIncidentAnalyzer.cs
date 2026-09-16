using DevOps.Application.Abstractions;
using DevOps.Application.Contracts;

namespace DevOps.Application.Analysis;

public sealed class UnavailableIncidentAnalyzer : IIncidentAnalyzer
{
    public Task<IncidentAnalysisModelResult> AnalyzeAsync(IncidentAnalysisContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new IncidentAnalysisModelResult(
            Succeeded: false,
            ModelName: null,
            Summary: "AI analysis is unavailable.",
            ProbableCause: "No model output was produced.",
            SeverityAssessment: context.Severity.ToString(),
            Evidence: [],
            RecommendedChecks: ["Confirm Ollama is running.", "Verify Ollama:BaseUrl and Ollama:Model."],
            SuggestedRemediation: [],
            Limitations:
            [
                "The local LLM was not reached.",
                "Monitoring and incident recording continue without AI."
            ],
            FailureReason: "Ollama is disabled or was not registered."));
    }
}
