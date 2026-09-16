using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DevOps.Application.Abstractions;
using DevOps.Application.Analysis;
using DevOps.Application.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOps.AI;

public sealed class OllamaIncidentAnalyzer(
    HttpClient httpClient,
    IOptions<OllamaOptions> options,
    ILogger<OllamaIncidentAnalyzer> logger) : IIncidentAnalyzer
{
    public async Task<IncidentAnalysisModelResult> AnalyzeAsync(
        IncidentAnalysisContext context,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            return await new UnavailableIncidentAnalyzer().AnalyzeAsync(context, cancellationToken);
        }

        var payload = new
        {
            model = settings.Model,
            stream = false,
            format = "json",
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = BuildUserPrompt(context) }
            }
        };

        try
        {
            using var response = await httpClient.PostAsJsonAsync("/api/chat", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Ollama returned {StatusCode}: {Body}", (int)response.StatusCode, Truncate(body));
                return Failure($"Ollama HTTP {(int)response.StatusCode}.", context);
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var content = document.RootElement.GetProperty("message").GetProperty("content").GetString();
            if (!IncidentAnalysisValidator.TryParse(content, out var parsed, out var error))
            {
                return Failure(error ?? "Malformed model output.", context);
            }

            return parsed with { ModelName = settings.Model };
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Ollama timed out");
            return Failure("Ollama request timed out.", context);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Ollama is unreachable");
            return Failure("Ollama is unreachable.", context);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Ollama returned invalid JSON");
            return Failure("Ollama returned invalid JSON.", context);
        }
    }

    private static IncidentAnalysisModelResult Failure(string reason, IncidentAnalysisContext context) =>
        new(
            false,
            null,
            "AI analysis is unavailable.",
            "No trusted model output was produced.",
            context.Severity.ToString(),
            [],
            ["Retry analysis after confirming Ollama is running."],
            [],
            [
                "The model was not used as a source of facts.",
                "Platform monitoring continues without this analysis."
            ],
            reason);

    private static string BuildUserPrompt(IncidentAnalysisContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine("CURRENT INCIDENT (observed evidence)");
        builder.AppendLine($"Service: {context.ServiceName}");
        builder.AppendLine($"Title: {context.IncidentTitle}");
        builder.AppendLine($"Description: {context.IncidentDescription}");
        builder.AppendLine($"Severity: {context.Severity}");
        builder.AppendLine($"DetectedAt: {context.DetectedAt:O}");
        builder.AppendLine($"ApplicationStatus: {context.ServiceStatus}");
        builder.AppendLine($"Container: {context.ContainerName} running={context.ContainerRunning} restarts={context.ContainerRestartCount} health={context.ContainerHealth}");
        builder.AppendLine("Recent health checks:");
        foreach (var line in context.RecentHealthMessages)
        {
            builder.AppendLine($"- {line}");
        }

        if (!string.IsNullOrWhiteSpace(context.LatestDeploymentSummary))
        {
            builder.AppendLine("Latest deployment:");
            builder.AppendLine(context.LatestDeploymentSummary);
        }

        if (!string.IsNullOrWhiteSpace(context.DeploymentCorrelation))
        {
            builder.AppendLine(context.DeploymentCorrelation);
        }

        builder.AppendLine();
        builder.AppendLine("HISTORICAL EXAMPLES (not current evidence; do not treat as proof of the current cause)");
        if (context.HistoricalExamples.Count == 0)
        {
            builder.AppendLine("None.");
        }
        else
        {
            foreach (var example in context.HistoricalExamples)
            {
                builder.AppendLine($"- {example.DetectedAt:u} {example.ServiceName} similarity={example.Similarity:P0} rootCause={example.RootCause} resolution={example.Resolution}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Return JSON with keys: summary, probableCause, severityAssessment, evidence, recommendedChecks, suggestedRemediation, limitations.");
        return builder.ToString();
    }

    private const string SystemPrompt =
        """
        You analyse infrastructure incidents for a DevOps platform.
        Rely only on supplied evidence. Do not invent logs, metrics, or events.
        Distinguish observed evidence from hypotheses.
        Identify uncertainty explicitly in limitations.
        You may recommend diagnostic checks. You cannot execute actions or run commands.
        Historical incidents are examples only, not current evidence.
        A nearby deployment is a potential correlation, not proof of causation.
        """;

    private static string Truncate(string value) =>
        value.Length <= 300 ? value : value[..300];
}
