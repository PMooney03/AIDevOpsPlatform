using DevOps.Application.Common;
using DevOps.Application.Contracts;
using DevOps.Application.Services;
using DevOps.Core.Entities;
using DevOps.Core.Enums;
using DevOps.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevOps.UnitTests.Services;

public class DeploymentServiceTests
{
    [Fact]
    public async Task Create_persists_deployment_for_existing_service()
    {
        var (service, serviceId, deployments) = await CreateSutAsync();
        var started = DateTimeOffset.UtcNow.AddMinutes(-10);

        var created = await service.CreateAsync(new CreateDeploymentRequest(
            serviceId,
            "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef",
            "main",
            DeploymentStageStatus.Succeeded,
            DeploymentStageStatus.Succeeded,
            DeploymentStageStatus.Succeeded,
            started,
            started.AddMinutes(4)), CancellationToken.None);

        Assert.Equal(serviceId, created.ServiceId);
        Assert.Single(deployments.Items);
        var listed = await service.GetForServiceAsync(serviceId, CancellationToken.None);
        Assert.Single(listed);
    }

    [Fact]
    public async Task Create_rejects_unknown_service()
    {
        var (service, _, _) = await CreateSutAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(new CreateDeploymentRequest(
                Guid.CreateVersion7(),
                "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef",
                "main",
                DeploymentStageStatus.Succeeded,
                DeploymentStageStatus.Succeeded,
                DeploymentStageStatus.Succeeded,
                DateTimeOffset.UtcNow,
                null), CancellationToken.None));
    }

    private static async Task<(DeploymentService Service, Guid ServiceId, FakeDeploymentRepository Deployments)> CreateSutAsync()
    {
        var services = new FakeMonitoredServiceRepository();
        var deployments = new FakeDeploymentRepository();
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
        var service = new DeploymentService(deployments, services, NullLogger<DeploymentService>.Instance);
        return (service, monitored.Id, deployments);
    }
}
