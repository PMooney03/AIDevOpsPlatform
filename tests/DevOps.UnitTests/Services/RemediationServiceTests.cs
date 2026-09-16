using DevOps.Application.Abstractions;
using DevOps.Application.Common;
using DevOps.Application.Contracts;
using DevOps.Application.Services;
using DevOps.Core.Entities;
using DevOps.Core.Enums;
using DevOps.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevOps.UnitTests.Services;

public class RemediationServiceTests
{
    [Fact]
    public async Task Unapproved_action_is_only_recommended()
    {
        var sut = await CreateSutAsync();
        var proposed = await sut.Service.ProposeAsync(
            sut.IncidentId,
            new CreateRemediationRequest(RemediationActionType.RunHealthCheck, null),
            CancellationToken.None);

        Assert.Equal(RemediationStatus.Recommended, proposed.Status);
        Assert.Equal(0, sut.Health.Checks);
    }

    [Fact]
    public async Task Rejected_action_cannot_execute()
    {
        var sut = await CreateSutAsync();
        var proposed = await sut.Service.ProposeAsync(
            sut.IncidentId,
            new CreateRemediationRequest(RemediationActionType.RunHealthCheck, null),
            CancellationToken.None);

        await sut.Service.RejectAsync(proposed.Id, "operator", new RejectRemediationRequest("not needed"), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.Service.ApproveAsync(proposed.Id, "operator", CancellationToken.None));
        Assert.Equal(0, sut.Health.Checks);
    }

    [Fact]
    public async Task Approved_health_check_executes()
    {
        var sut = await CreateSutAsync();
        var proposed = await sut.Service.ProposeAsync(
            sut.IncidentId,
            new CreateRemediationRequest(RemediationActionType.RunHealthCheck, "Probe the service"),
            CancellationToken.None);

        var executed = await sut.Service.ApproveAsync(proposed.Id, "operator", CancellationToken.None);

        Assert.Equal(RemediationStatus.Completed, executed.Status);
        Assert.Equal(1, sut.Health.Checks);
        Assert.Equal("operator", executed.ApprovedBy);
    }

    [Fact]
    public async Task Duplicate_approval_is_rejected()
    {
        var sut = await CreateSutAsync();
        var proposed = await sut.Service.ProposeAsync(
            sut.IncidentId,
            new CreateRemediationRequest(RemediationActionType.RunHealthCheck, null),
            CancellationToken.None);
        await sut.Service.ApproveAsync(proposed.Id, "operator", CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.Service.ApproveAsync(proposed.Id, "operator", CancellationToken.None));
    }

    [Fact]
    public async Task Restart_requires_associated_container()
    {
        var sut = await CreateSutAsync(containerName: null);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            sut.Service.ProposeAsync(
                sut.IncidentId,
                new CreateRemediationRequest(RemediationActionType.RestartContainer, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Approved_restart_uses_service_container_name()
    {
        var sut = await CreateSutAsync(containerName: "demo-unhealthy");
        var proposed = await sut.Service.ProposeAsync(
            sut.IncidentId,
            new CreateRemediationRequest(RemediationActionType.RestartContainer, null),
            CancellationToken.None);

        var executed = await sut.Service.ApproveAsync(proposed.Id, "operator", CancellationToken.None);

        Assert.Equal(1, sut.Containers.RestartCalls);
        Assert.Equal(RemediationStatus.Completed, executed.Status);
    }

    [Fact]
    public async Task Failed_restart_is_recorded()
    {
        var sut = await CreateSutAsync(containerName: "demo-unhealthy");
        sut.Containers.RestartSucceeded = false;
        sut.Containers.RestartMessage = "boom";
        var proposed = await sut.Service.ProposeAsync(
            sut.IncidentId,
            new CreateRemediationRequest(RemediationActionType.RestartContainer, null),
            CancellationToken.None);

        var executed = await sut.Service.ApproveAsync(proposed.Id, "operator", CancellationToken.None);
        Assert.Equal(RemediationStatus.Failed, executed.Status);
        Assert.Equal("boom", executed.FailureReason);
    }

    private static async Task<Sut> CreateSutAsync(string? containerName = "demo-unhealthy")
    {
        var services = new FakeMonitoredServiceRepository();
        var incidents = new FakeIncidentRepository();
        var remediations = new FakeRemediationRepository();
        var containers = new FakeContainerMonitor();
        var health = new FakeHealthMonitor();
        var now = DateTimeOffset.UtcNow;
        var monitored = new MonitoredService
        {
            Id = Guid.CreateVersion7(),
            Name = "payments-api",
            BaseUrl = "https://payments.internal",
            HealthEndpoint = "/health",
            ContainerName = containerName,
            CreatedAt = now,
            UpdatedAt = now
        };
        await services.AddAsync(monitored, CancellationToken.None);
        var incident = new Incident
        {
            Id = Guid.CreateVersion7(),
            ServiceId = monitored.Id,
            Title = "payments-api is Unhealthy",
            DetectedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        await incidents.AddAsync(incident, CancellationToken.None);
        var service = new RemediationService(
            remediations,
            incidents,
            services,
            containers,
            health,
            NullLogger<RemediationService>.Instance);
        return new Sut(service, incident.Id, health, containers);
    }

    private sealed record Sut(
        RemediationService Service,
        Guid IncidentId,
        FakeHealthMonitor Health,
        FakeContainerMonitor Containers);

    private sealed class FakeHealthMonitor : IServiceHealthMonitor
    {
        public int Checks { get; private set; }

        public Task MonitorEnabledServicesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task CheckServiceAsync(Guid serviceId, CancellationToken cancellationToken)
        {
            Checks++;
            return Task.CompletedTask;
        }
    }
}
