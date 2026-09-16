using System.Text.Json;
using DevOps.Application.Abstractions;
using DevOps.Application.Analysis;
using DevOps.Application.Common;
using DevOps.Application.Contracts;
using DevOps.Application.Mapping;
using DevOps.Core.Entities;
using DevOps.Core.Enums;
using Microsoft.Extensions.Logging;

namespace DevOps.Application.Services;

public sealed class IncidentAnalysisService(
    IIncidentRepository incidents,
    IMonitoredServiceRepository services,
    IHealthCheckResultRepository healthChecks,
    IDeploymentRepository deployments,
    IAiAnalysisRepository analyses,
    IIncidentAnalyzer analyzer,
    ILogger<IncidentAnalysisService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public async Task<IncidentAnalysisResponse> AnalyzeAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        var incident = await incidents.GetByIdAsync(incidentId, cancellationToken)
            ?? throw new NotFoundException($"Incident '{incidentId}' was not found.");

        var service = await services.GetByIdAsync(incident.ServiceId, cancellationToken)
            ?? throw new NotFoundException($"Service '{incident.ServiceId}' was not found.");

        var history = await healthChecks.GetHistoryAsync(incident.ServiceId, 10, cancellationToken);
        var resolved = await incidents.GetResolvedAsync(cancellationToken);
        var similar = IncidentSimilarityScorer.Rank(incident, resolved, 0.12, 5);
        var latestDeployment = await deployments.GetLatestForServiceAsync(incident.ServiceId, cancellationToken);

        var similarExamples = new List<SimilarIncidentExample>();
        foreach (var item in similar)
        {
            var similarService = await services.GetByIdAsync(item.Incident.ServiceId, cancellationToken);
            similarExamples.Add(new SimilarIncidentExample(
                item.Incident.Id,
                similarService?.Name ?? "unknown",
                item.Incident.DetectedAt,
                item.Score,
                item.Incident.RootCause,
                item.Incident.Resolution));
        }

        var context = new IncidentAnalysisContext(
            incident.Id,
            incident.Title,
            incident.Description,
            incident.Severity,
            incident.DetectedAt,
            service.Name,
            service.Status,
            service.ContainerName,
            service.ContainerRunning,
            service.ContainerRestartCount,
            service.ContainerHealth,
            history.Select(item => $"{item.CheckedAt:O} {item.Status} HTTP {item.HttpStatusCode} {item.Message}").ToArray(),
            similarExamples,
            FormatDeployment(latestDeployment),
            BuildCorrelation(incident, latestDeployment));

        IncidentAnalysisModelResult modelResult;
        try
        {
            modelResult = await analyzer.AnalyzeAsync(context, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Incident analysis failed for {IncidentId}", incidentId);
            modelResult = new IncidentAnalysisModelResult(
                false,
                null,
                "AI analysis failed.",
                "No model output was produced.",
                incident.Severity.ToString(),
                [],
                [],
                [],
                ["The analyzer threw an exception. Monitoring continues."],
                exception.Message);
        }

        var analysis = new AiAnalysis
        {
            Id = Guid.CreateVersion7(),
            IncidentId = incident.Id,
            Succeeded = modelResult.Succeeded,
            ModelName = modelResult.ModelName,
            Summary = modelResult.Summary,
            ProbableCause = modelResult.ProbableCause,
            SeverityAssessment = modelResult.SeverityAssessment,
            EvidenceJson = JsonSerializer.Serialize(modelResult.Evidence, JsonOptions),
            RecommendedChecksJson = JsonSerializer.Serialize(modelResult.RecommendedChecks, JsonOptions),
            SuggestedRemediationJson = JsonSerializer.Serialize(modelResult.SuggestedRemediation, JsonOptions),
            LimitationsJson = JsonSerializer.Serialize(modelResult.Limitations, JsonOptions),
            FailureReason = modelResult.FailureReason,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await analyses.AddAsync(analysis, cancellationToken);
        await analyses.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Stored AI analysis {AnalysisId} for incident {IncidentId} succeeded {Succeeded}",
            analysis.Id,
            incident.Id,
            analysis.Succeeded);

        return ToResponse(analysis);
    }

    public async Task<IncidentAnalysisResponse?> GetLatestAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        _ = await incidents.GetByIdAsync(incidentId, cancellationToken)
            ?? throw new NotFoundException($"Incident '{incidentId}' was not found.");

        var latest = await analyses.GetLatestForIncidentAsync(incidentId, cancellationToken);
        return latest is null ? null : ToResponse(latest);
    }

    public async Task<IncidentResponse> ResolveAsync(Guid incidentId, ResolveIncidentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Resolution))
        {
            throw new DomainValidationException("resolution", "Resolution summary is required.");
        }

        var incident = await incidents.GetByIdAsync(incidentId, cancellationToken)
            ?? throw new NotFoundException($"Incident '{incidentId}' was not found.");

        var now = DateTimeOffset.UtcNow;
        incident.Resolution = request.Resolution.Trim();
        incident.RootCause = string.IsNullOrWhiteSpace(request.RootCause) ? null : request.RootCause.Trim();
        incident.ActionsTaken = string.IsNullOrWhiteSpace(request.ActionsTaken) ? null : request.ActionsTaken.Trim();
        incident.ApplyStatus(IncidentStatus.Resolved, now);
        incident.UpdatedAt = now;
        await incidents.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Resolved incident {IncidentId}", incidentId);
        return incident.ToResponse();
    }

    public async Task<IReadOnlyList<SimilarIncidentResponse>> GetSimilarAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        var incident = await incidents.GetByIdAsync(incidentId, cancellationToken)
            ?? throw new NotFoundException($"Incident '{incidentId}' was not found.");

        var resolved = await incidents.GetResolvedAsync(cancellationToken);
        var ranked = IncidentSimilarityScorer.Rank(incident, resolved, 0.12, 8);
        var results = new List<SimilarIncidentResponse>();
        foreach (var item in ranked)
        {
            var service = await services.GetByIdAsync(item.Incident.ServiceId, cancellationToken);
            results.Add(new SimilarIncidentResponse(
                item.Incident.Id,
                item.Incident.ServiceId,
                service?.Name ?? "unknown",
                item.Incident.DetectedAt,
                Math.Round(item.Score, 4),
                item.Incident.RootCause,
                item.Incident.Resolution));
        }

        return results;
    }

    private static IncidentAnalysisResponse ToResponse(AiAnalysis analysis) =>
        new(
            analysis.Id,
            analysis.IncidentId,
            analysis.Succeeded,
            analysis.ModelName,
            analysis.Summary,
            analysis.ProbableCause,
            analysis.SeverityAssessment,
            DeserializeList(analysis.EvidenceJson),
            DeserializeList(analysis.RecommendedChecksJson),
            DeserializeList(analysis.SuggestedRemediationJson),
            DeserializeList(analysis.LimitationsJson),
            analysis.FailureReason,
            analysis.CreatedAt);

    private static IReadOnlyList<string> DeserializeList(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? FormatDeployment(Deployment? deployment)
    {
        if (deployment is null)
        {
            return null;
        }

        return $"Commit {deployment.CommitSha} on {deployment.Branch}. Build {deployment.BuildStatus}, tests {deployment.TestStatus}, deploy {deployment.DeploymentStatus}. Started {deployment.StartedAt:O}.";
    }

    private static string? BuildCorrelation(Incident incident, Deployment? deployment)
    {
        if (deployment is null)
        {
            return null;
        }

        var marker = deployment.CompletedAt ?? deployment.StartedAt;
        var delta = incident.DetectedAt - marker;
        if (delta < TimeSpan.Zero || delta > TimeSpan.FromMinutes(30))
        {
            return null;
        }

        return $"Potential correlation: incident began {delta.TotalMinutes:0} minutes after deployment {deployment.CommitSha}. This does not prove the deployment caused the incident.";
    }
}
