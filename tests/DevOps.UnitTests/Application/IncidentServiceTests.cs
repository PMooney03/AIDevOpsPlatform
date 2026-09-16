using DevOps.Application.Common;
using DevOps.Application.Contracts;
using DevOps.Application.Services;
using DevOps.Core.Enums;
using DevOps.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevOps.UnitTests.Application;

public class IncidentServiceTests
{
    [Fact]
    public async Task CreateAsync_opens_an_incident_for_an_existing_service()
    {
        var (incidentService, serviceId) = await CreateSutWithServiceAsync();

        var created = await incidentService.CreateAsync(new CreateIncidentRequest(
            serviceId,
            "Database connectivity failure",
            "Timeouts started at 19:42",
            IncidentSeverity.High), CancellationToken.None);

        Assert.Equal(IncidentStatus.Open, created.Status);
        Assert.Equal(IncidentSeverity.High, created.Severity);
        Assert.Equal(serviceId, created.ServiceId);
        Assert.Null(created.ResolvedAt);
    }

    [Fact]
    public async Task CreateAsync_rejects_missing_title()
    {
        var (incidentService, serviceId) = await CreateSutWithServiceAsync();

        await Assert.ThrowsAsync<DomainValidationException>(() => incidentService.CreateAsync(
            new CreateIncidentRequest(serviceId, " ", null, IncidentSeverity.Low),
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_rejects_unknown_service()
    {
        var (incidentService, _) = await CreateSutWithServiceAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => incidentService.CreateAsync(
            new CreateIncidentRequest(Guid.CreateVersion7(), "Outage", null, IncidentSeverity.Critical),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_can_resolve_an_incident()
    {
        var (incidentService, serviceId) = await CreateSutWithServiceAsync();
        var created = await incidentService.CreateAsync(new CreateIncidentRequest(
            serviceId,
            "Database connectivity failure",
            null,
            IncidentSeverity.High), CancellationToken.None);

        var updated = await incidentService.UpdateAsync(created.Id, new UpdateIncidentRequest(
            created.Title,
            created.Description,
            IncidentSeverity.Medium,
            IncidentStatus.Resolved,
            "Restored database connectivity"), CancellationToken.None);

        Assert.Equal(IncidentStatus.Resolved, updated.Status);
        Assert.Equal(IncidentSeverity.Medium, updated.Severity);
        Assert.NotNull(updated.ResolvedAt);
        Assert.Equal("Restored database connectivity", updated.Resolution);
    }

    private static async Task<(IncidentService Service, Guid ServiceId)> CreateSutWithServiceAsync()
    {
        var services = new FakeMonitoredServiceRepository();
        var incidents = new FakeIncidentRepository();
        var monitoredServiceService = new MonitoredServiceService(
            services,
            incidents,
            NullLogger<MonitoredServiceService>.Instance);

        var created = await monitoredServiceService.CreateAsync(new CreateMonitoredServiceRequest(
            "payments-api",
            null,
            "https://payments.internal",
            "/health"), CancellationToken.None);

        return (new IncidentService(incidents, services, new FakePlatformMetrics(), NullLogger<IncidentService>.Instance), created.Id);
    }
}
