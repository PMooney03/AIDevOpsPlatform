using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevOps.Application.Contracts;
using DevOps.Core.Enums;

namespace DevOps.IntegrationTests;

public class ApiEndpointTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DevOpsApiFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Service_crud_round_trip_works()
    {
        var createdResponse = await _client.PostAsJsonAsync("/api/services", new CreateMonitoredServiceRequest(
            "payments-api",
            "Payments HTTP API",
            "https://payments.internal",
            "/health"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);

        var created = await createdResponse.Content.ReadFromJsonAsync<MonitoredServiceResponse>(JsonOptions);
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/services/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updatedResponse = await _client.PutAsJsonAsync($"/api/services/{created.Id}", new UpdateMonitoredServiceRequest(
            "payments-api",
            "Updated description",
            "https://payments.internal",
            "/ready",
            false), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);

        var updated = await updatedResponse.Content.ReadFromJsonAsync<MonitoredServiceResponse>(JsonOptions);
        Assert.Equal("/ready", updated!.HealthEndpoint);
        Assert.False(updated.MonitoringEnabled);

        var listResponse = await _client.GetAsync("/api/services");
        var list = await listResponse.Content.ReadFromJsonAsync<List<MonitoredServiceResponse>>(JsonOptions);
        Assert.Contains(list!, item => item.Id == created.Id);

        var deleteResponse = await _client.DeleteAsync($"/api/services/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var missing = await _client.GetAsync($"/api/services/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Invalid_service_returns_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/api/services", new CreateMonitoredServiceRequest(
            "",
            null,
            "not-a-url",
            ""), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Incident_can_be_created_and_updated()
    {
        var serviceResponse = await _client.PostAsJsonAsync("/api/services", new CreateMonitoredServiceRequest(
            "notifications-api",
            null,
            "https://notifications.internal",
            "/health"), JsonOptions);
        var service = await serviceResponse.Content.ReadFromJsonAsync<MonitoredServiceResponse>(JsonOptions);

        var createIncident = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest(
            service!.Id,
            "Elevated error rate",
            "HTTP 500 responses increased",
            IncidentSeverity.High), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createIncident.StatusCode);
        var incident = await createIncident.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);
        Assert.Equal(IncidentStatus.Open, incident!.Status);

        var updateIncident = await _client.PutAsJsonAsync($"/api/incidents/{incident.Id}", new UpdateIncidentRequest(
            incident.Title,
            incident.Description,
            IncidentSeverity.Critical,
            IncidentStatus.Investigating,
            null), JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateIncident.StatusCode);
        var updated = await updateIncident.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);
        Assert.Equal(IncidentStatus.Investigating, updated!.Status);
        Assert.Equal(IncidentSeverity.Critical, updated.Severity);
        Assert.Null(updated.ResolvedAt);
    }

    [Fact]
    public async Task Unknown_service_incident_returns_not_found()
    {
        var response = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest(
            Guid.CreateVersion7(),
            "Ghost incident",
            null,
            IncidentSeverity.Low), JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Service_health_history_is_empty_until_checks_exist()
    {
        var createdResponse = await _client.PostAsJsonAsync("/api/services", new CreateMonitoredServiceRequest(
            "inventory-api",
            null,
            "https://inventory.internal",
            "/health"), JsonOptions);
        var created = await createdResponse.Content.ReadFromJsonAsync<MonitoredServiceResponse>(JsonOptions);

        var latest = await _client.GetAsync($"/api/services/{created!.Id}/health");
        Assert.Equal(HttpStatusCode.NotFound, latest.StatusCode);

        var history = await _client.GetAsync($"/api/services/{created.Id}/health/history");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        var items = await history.Content.ReadFromJsonAsync<List<HealthCheckResultResponse>>(JsonOptions);
        Assert.Empty(items!);
    }

    [Fact]
    public async Task Service_runtime_is_available_without_container_association()
    {
        var createdResponse = await _client.PostAsJsonAsync("/api/services", new CreateMonitoredServiceRequest(
            "billing-api",
            null,
            "https://billing.internal",
            "/health"), JsonOptions);
        var created = await createdResponse.Content.ReadFromJsonAsync<MonitoredServiceResponse>(JsonOptions);

        var runtimeResponse = await _client.GetAsync($"/api/services/{created!.Id}/runtime");
        Assert.Equal(HttpStatusCode.OK, runtimeResponse.StatusCode);
        var runtime = await runtimeResponse.Content.ReadFromJsonAsync<ServiceRuntimeResponse>(JsonOptions);
        Assert.Equal(created.Id, runtime!.ServiceId);
        Assert.Null(runtime.Container);
        Assert.Null(runtime.LatestHealth);
    }

    [Fact]
    public async Task Metrics_endpoint_exposes_prometheus_text()
    {
        var response = await _client.GetAsync("/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("# TYPE", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Analyze_stores_unavailable_result_when_ollama_is_disabled()
    {
        var serviceResponse = await _client.PostAsJsonAsync("/api/services", new CreateMonitoredServiceRequest(
            "catalog-api",
            null,
            "https://catalog.internal",
            "/health"), JsonOptions);
        var service = await serviceResponse.Content.ReadFromJsonAsync<MonitoredServiceResponse>(JsonOptions);

        var incidentResponse = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest(
            service!.Id,
            "Database connection timeout",
            "PostgreSQL connectivity failure",
            IncidentSeverity.High), JsonOptions);
        var incident = await incidentResponse.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);

        var analyze = await _client.PostAsync($"/api/incidents/{incident!.Id}/analyze", null);
        Assert.Equal(HttpStatusCode.OK, analyze.StatusCode);
        var analysis = await analyze.Content.ReadFromJsonAsync<IncidentAnalysisResponse>(JsonOptions);
        Assert.False(analysis!.Succeeded);
        Assert.Contains("unavailable", analysis.Summary, StringComparison.OrdinalIgnoreCase);

        var latest = await _client.GetAsync($"/api/incidents/{incident.Id}/analysis");
        Assert.Equal(HttpStatusCode.OK, latest.StatusCode);
    }

    [Fact]
    public async Task Resolve_and_similar_use_historical_memory()
    {
        var serviceResponse = await _client.PostAsJsonAsync("/api/services", new CreateMonitoredServiceRequest(
            "ledger-api",
            null,
            "https://ledger.internal",
            "/health"), JsonOptions);
        var service = await serviceResponse.Content.ReadFromJsonAsync<MonitoredServiceResponse>(JsonOptions);

        var historicalResponse = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest(
            service!.Id,
            "Database connection timeout",
            "PostgreSQL hostname incorrect after deployment",
            IncidentSeverity.High), JsonOptions);
        var historical = await historicalResponse.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);

        var resolve = await _client.PostAsJsonAsync($"/api/incidents/{historical!.Id}/resolve", new ResolveIncidentRequest(
            "DB_HOST configuration corrected.",
            "Incorrect database hostname following deployment.",
            "Restored environment variables."), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, resolve.StatusCode);

        var currentResponse = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest(
            service.Id,
            "Database connection timeout",
            "PostgreSQL connectivity failure",
            IncidentSeverity.High), JsonOptions);
        var current = await currentResponse.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);

        var similar = await _client.GetFromJsonAsync<List<SimilarIncidentResponse>>(
            $"/api/incidents/{current!.Id}/similar",
            JsonOptions);
        Assert.Contains(similar!, item => item.IncidentId == historical.Id);
    }

    [Fact]
    public async Task Deployment_round_trip_works()
    {
        var serviceResponse = await _client.PostAsJsonAsync("/api/services", new CreateMonitoredServiceRequest(
            "shipping-api",
            null,
            "https://shipping.internal",
            "/health"), JsonOptions);
        var service = await serviceResponse.Content.ReadFromJsonAsync<MonitoredServiceResponse>(JsonOptions);
        var started = DateTimeOffset.UtcNow.AddMinutes(-8);

        var createdResponse = await _client.PostAsJsonAsync("/api/deployments", new CreateDeploymentRequest(
            service!.Id,
            "abcdefabcdefabcdefabcdefabcdefabcdefabcd",
            "main",
            DeploymentStageStatus.Succeeded,
            DeploymentStageStatus.Succeeded,
            DeploymentStageStatus.Succeeded,
            started,
            started.AddMinutes(3)), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);

        var list = await _client.GetFromJsonAsync<List<DeploymentResponse>>(
            $"/api/services/{service.Id}/deployments",
            JsonOptions);
        Assert.Single(list!);
        Assert.Equal("main", list![0].Branch);
    }

    [Fact]
    public async Task Overview_and_live_health_are_available()
    {
        var live = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);

        var overview = await _client.GetFromJsonAsync<OverviewResponse>("/api/overview", JsonOptions);
        Assert.NotNull(overview);
        Assert.True(overview.TotalServices >= 0);
    }

    [Fact]
    public async Task Remediation_health_check_requires_approval()
    {
        var serviceResponse = await _client.PostAsJsonAsync("/api/services", new CreateMonitoredServiceRequest(
            "chaos-api",
            null,
            "https://chaos.internal",
            "/health"), JsonOptions);
        var service = await serviceResponse.Content.ReadFromJsonAsync<MonitoredServiceResponse>(JsonOptions);

        var incidentResponse = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest(
            service!.Id,
            "chaos-api is Unhealthy",
            "HTTP 500",
            IncidentSeverity.High), JsonOptions);
        var incident = await incidentResponse.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);

        var proposedResponse = await _client.PostAsJsonAsync(
            $"/api/incidents/{incident!.Id}/remediations",
            new CreateRemediationRequest(RemediationActionType.RunHealthCheck, "On-demand probe"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, proposedResponse.StatusCode);
        var proposed = await proposedResponse.Content.ReadFromJsonAsync<RemediationResponse>(JsonOptions);
        Assert.Equal(RemediationStatus.Recommended, proposed!.Status);

        var approved = await _client.PostAsJsonAsync($"/api/remediations/{proposed.Id}/approve", new { }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        var completed = await approved.Content.ReadFromJsonAsync<RemediationResponse>(JsonOptions);
        Assert.Equal(RemediationStatus.Completed, completed!.Status);

        var second = await _client.PostAsJsonAsync($"/api/remediations/{proposed.Id}/approve", new { }, JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
