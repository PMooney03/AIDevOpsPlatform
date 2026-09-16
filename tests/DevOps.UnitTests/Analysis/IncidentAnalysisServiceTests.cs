using DevOps.Application.Analysis;
using DevOps.Application.Contracts;
using DevOps.Application.Services;
using DevOps.Core.Entities;
using DevOps.Core.Enums;
using DevOps.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevOps.UnitTests.Analysis;

public class IncidentAnalysisServiceTests
{
    [Fact]
    public async Task Analyze_stores_unavailable_result_without_throwing()
    {
        var (service, incidentId, analyses) = await CreateSutAsync();

        var result = await service.AnalyzeAsync(incidentId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Single(analyses.Items);
        Assert.Contains("unavailable", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_latest_returns_null_before_analysis_is_stored()
    {
        var (service, incidentId, _) = await CreateSutAsync();

        var latest = await service.GetLatestAsync(incidentId, CancellationToken.None);

        Assert.Null(latest);
    }

    [Fact]
    public async Task Resolve_requires_summary_and_marks_resolved()
    {
        var (service, incidentId, _) = await CreateSutAsync();

        var resolved = await service.ResolveAsync(incidentId, new ResolveIncidentRequest(
            "Restored database connectivity",
            "Incorrect DB_HOST",
            "Corrected configuration"), CancellationToken.None);

        Assert.Equal(IncidentStatus.Resolved, resolved.Status);
        Assert.Equal("Incorrect DB_HOST", resolved.RootCause);
        await Assert.ThrowsAsync<DevOps.Application.Common.DomainValidationException>(() =>
            service.ResolveAsync(incidentId, new ResolveIncidentRequest(" ", null, null), CancellationToken.None));
    }

    [Fact]
    public async Task Similar_returns_historical_resolved_match()
    {
        var services = new FakeMonitoredServiceRepository();
        var incidents = new FakeIncidentRepository();
        var health = new FakeHealthCheckResultRepository();
        var deployments = new FakeDeploymentRepository();
        var analyses = new FakeAiAnalysisRepository();
        var now = DateTimeOffset.UtcNow;
        var monitored = new MonitoredService
        {
            Id = Guid.CreateVersion7(),
            Name = "payments-api",
            BaseUrl = "https://payments.internal",
            HealthEndpoint = "/health",
            CreatedAt = now,
            UpdatedAt = now
        };
        await services.AddAsync(monitored, CancellationToken.None);

        var historical = new Incident
        {
            Id = Guid.CreateVersion7(),
            ServiceId = monitored.Id,
            Title = "Database connection timeout",
            Description = "PostgreSQL hostname incorrect after deployment",
            Status = IncidentStatus.Resolved,
            RootCause = "Incorrect database hostname following deployment.",
            Resolution = "DB_HOST configuration corrected.",
            DetectedAt = now.AddDays(-8),
            CreatedAt = now.AddDays(-8),
            UpdatedAt = now.AddDays(-8)
        };
        var current = new Incident
        {
            Id = Guid.CreateVersion7(),
            ServiceId = monitored.Id,
            Title = "Database connection timeout",
            Description = "PostgreSQL connectivity failure",
            DetectedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        await incidents.AddAsync(historical, CancellationToken.None);
        await incidents.AddAsync(current, CancellationToken.None);

        var analysis = new IncidentAnalysisService(
            incidents,
            services,
            health,
            deployments,
            analyses,
            new UnavailableIncidentAnalyzer(),
            NullLogger<IncidentAnalysisService>.Instance);

        var similar = await analysis.GetSimilarAsync(current.Id, CancellationToken.None);
        Assert.Contains(similar, item => item.IncidentId == historical.Id);
    }

    private static async Task<(IncidentAnalysisService Service, Guid IncidentId, FakeAiAnalysisRepository Analyses)> CreateSutAsync()
    {
        var services = new FakeMonitoredServiceRepository();
        var incidents = new FakeIncidentRepository();
        var health = new FakeHealthCheckResultRepository();
        var deployments = new FakeDeploymentRepository();
        var analyses = new FakeAiAnalysisRepository();
        var now = DateTimeOffset.UtcNow;
        var monitored = new MonitoredService
        {
            Id = Guid.CreateVersion7(),
            Name = "payments-api",
            BaseUrl = "https://payments.internal",
            HealthEndpoint = "/health",
            CreatedAt = now,
            UpdatedAt = now
        };
        await services.AddAsync(monitored, CancellationToken.None);
        var incident = new Incident
        {
            Id = Guid.CreateVersion7(),
            ServiceId = monitored.Id,
            Title = "Simulated database connectivity incident",
            Description = "Timeouts started after deployment",
            Severity = IncidentSeverity.High,
            DetectedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        await incidents.AddAsync(incident, CancellationToken.None);
        var service = new IncidentAnalysisService(
            incidents,
            services,
            health,
            deployments,
            analyses,
            new UnavailableIncidentAnalyzer(),
            NullLogger<IncidentAnalysisService>.Instance);
        return (service, incident.Id, analyses);
    }
}
