using DevOps.Application.Common;
using DevOps.Application.Contracts;
using DevOps.Application.Services;
using DevOps.Core.Enums;
using DevOps.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevOps.UnitTests.Application;

public class MonitoredServiceServiceTests
{
    [Fact]
    public async Task CreateAsync_stores_a_valid_service()
    {
        var service = CreateSut(out var services, out _);

        var created = await service.CreateAsync(new CreateMonitoredServiceRequest(
            "payments-api",
            "Payments HTTP API",
            "https://payments.internal/",
            "/health"), CancellationToken.None);

        Assert.Equal("payments-api", created.Name);
        Assert.Equal("https://payments.internal", created.BaseUrl);
        Assert.Equal(ServiceStatus.Unknown, created.Status);
        Assert.True(created.MonitoringEnabled);
        Assert.Equal(created.Id, (await services.GetByIdAsync(created.Id, CancellationToken.None))!.Id);
    }

    [Theory]
    [InlineData("", "https://payments.internal", "/health")]
    [InlineData("payments-api", "not-a-url", "/health")]
    [InlineData("payments-api", "ftp://payments.internal", "/health")]
    [InlineData("payments-api", "https://payments.internal", "")]
    public async Task CreateAsync_rejects_invalid_registration(string name, string baseUrl, string healthEndpoint)
    {
        var service = CreateSut(out _, out _);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateAsync(
            new CreateMonitoredServiceRequest(name, null, baseUrl, healthEndpoint),
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_names()
    {
        var service = CreateSut(out _, out _);
        var request = new CreateMonitoredServiceRequest(
            "payments-api",
            null,
            "https://payments.internal",
            "/health");

        await service.CreateAsync(request, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_is_blocked_when_incident_history_exists()
    {
        var service = CreateSut(out _, out var incidents);
        var created = await service.CreateAsync(new CreateMonitoredServiceRequest(
            "payments-api",
            null,
            "https://payments.internal",
            "/health"), CancellationToken.None);

        await incidents.AddAsync(new DevOps.Core.Entities.Incident
        {
            Id = Guid.CreateVersion7(),
            ServiceId = created.Id,
            Title = "HTTP 500 spike",
            Severity = IncidentSeverity.High,
            DetectedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.DeleteAsync(created.Id, CancellationToken.None));
    }

    private static MonitoredServiceService CreateSut(
        out FakeMonitoredServiceRepository services,
        out FakeIncidentRepository incidents)
    {
        services = new FakeMonitoredServiceRepository();
        incidents = new FakeIncidentRepository();
        return new MonitoredServiceService(services, incidents, NullLogger<MonitoredServiceService>.Instance);
    }
}
