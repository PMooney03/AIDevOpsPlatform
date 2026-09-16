using DevOps.Application.Abstractions;
using DevOps.Application.Common;
using DevOps.Application.Contracts;
using DevOps.Core.Entities;
using DevOps.Core.Enums;
using Microsoft.Extensions.Logging;

namespace DevOps.Application.Services;

public sealed class DeploymentService(
    IDeploymentRepository deployments,
    IMonitoredServiceRepository services,
    ILogger<DeploymentService> logger)
{
    public async Task<DeploymentResponse> CreateAsync(CreateDeploymentRequest request, CancellationToken cancellationToken)
    {
        if (request.ServiceId == Guid.Empty)
        {
            throw new DomainValidationException("serviceId", "Service ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CommitSha))
        {
            throw new DomainValidationException("commitSha", "Commit SHA is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Branch))
        {
            throw new DomainValidationException("branch", "Branch is required.");
        }

        if (!await services.ExistsAsync(request.ServiceId, cancellationToken))
        {
            throw new NotFoundException($"Service '{request.ServiceId}' was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var deployment = new Deployment
        {
            Id = Guid.CreateVersion7(),
            ServiceId = request.ServiceId,
            CommitSha = request.CommitSha.Trim(),
            Branch = request.Branch.Trim(),
            BuildStatus = request.BuildStatus,
            TestStatus = request.TestStatus,
            DeploymentStatus = request.DeploymentStatus,
            StartedAt = request.StartedAt == default ? now : request.StartedAt,
            CompletedAt = request.CompletedAt,
            CreatedAt = now
        };

        await deployments.AddAsync(deployment, cancellationToken);
        await deployments.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Recorded deployment {DeploymentId} for service {ServiceId} commit {CommitSha}",
            deployment.Id,
            deployment.ServiceId,
            deployment.CommitSha);

        return ToResponse(deployment);
    }

    public async Task<IReadOnlyList<DeploymentResponse>> GetForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        if (!await services.ExistsAsync(serviceId, cancellationToken))
        {
            throw new NotFoundException($"Service '{serviceId}' was not found.");
        }

        var items = await deployments.GetForServiceAsync(serviceId, cancellationToken);
        return items.Select(ToResponse).ToArray();
    }

    private static DeploymentResponse ToResponse(Deployment deployment) =>
        new(
            deployment.Id,
            deployment.ServiceId,
            deployment.CommitSha,
            deployment.Branch,
            deployment.BuildStatus,
            deployment.TestStatus,
            deployment.DeploymentStatus,
            deployment.StartedAt,
            deployment.CompletedAt,
            deployment.CreatedAt);
}
