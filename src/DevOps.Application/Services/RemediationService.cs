using DevOps.Application.Abstractions;
using DevOps.Application.Common;
using DevOps.Application.Contracts;
using DevOps.Application.Monitoring;
using DevOps.Core.Entities;
using DevOps.Core.Enums;
using Microsoft.Extensions.Logging;

namespace DevOps.Application.Services;

public sealed class RemediationService(
    IRemediationRepository remediations,
    IIncidentRepository incidents,
    IMonitoredServiceRepository services,
    IContainerMonitor containers,
    IServiceHealthMonitor healthMonitor,
    ILogger<RemediationService> logger)
{
    public async Task<RemediationResponse> ProposeAsync(
        Guid incidentId,
        CreateRemediationRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.ActionType))
        {
            throw new DomainValidationException("actionType", "Action type is not a supported remediation.");
        }

        var incident = await incidents.GetByIdAsync(incidentId, cancellationToken)
            ?? throw new NotFoundException($"Incident '{incidentId}' was not found.");

        var service = await services.GetByIdAsync(incident.ServiceId, cancellationToken)
            ?? throw new NotFoundException($"Service '{incident.ServiceId}' was not found.");

        if (request.ActionType is RemediationActionType.RestartContainer
                or RemediationActionType.RefreshContainerStatus
                or RemediationActionType.CollectRecentLogs
            && string.IsNullOrWhiteSpace(service.ContainerName))
        {
            throw new DomainValidationException(
                "actionType",
                "This service has no associated container name, so that remediation cannot be proposed.");
        }

        var now = DateTimeOffset.UtcNow;
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? DefaultDescription(request.ActionType, service)
            : request.Description.Trim();

        var action = new RemediationAction
        {
            Id = Guid.CreateVersion7(),
            IncidentId = incident.Id,
            ServiceId = service.Id,
            ActionType = request.ActionType,
            Description = description,
            Status = RemediationStatus.Recommended,
            RequestedAt = now,
            CreatedAt = now
        };

        await remediations.AddAsync(action, cancellationToken);
        await remediations.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Proposed remediation {RemediationId} {ActionType} for incident {IncidentId}",
            action.Id,
            action.ActionType,
            incident.Id);
        return ToResponse(action);
    }

    public async Task<RemediationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var action = await remediations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Remediation '{id}' was not found.");
        return ToResponse(action);
    }

    public async Task<IReadOnlyList<RemediationResponse>> GetForIncidentAsync(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        if (await incidents.GetByIdAsync(incidentId, cancellationToken) is null)
        {
            throw new NotFoundException($"Incident '{incidentId}' was not found.");
        }

        var items = await remediations.GetForIncidentAsync(incidentId, cancellationToken);
        return items.Select(ToResponse).ToArray();
    }

    public async Task<RemediationResponse> ApproveAsync(
        Guid id,
        string approvedBy,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(approvedBy))
        {
            throw new DomainValidationException("approvedBy", "An approver identity is required.");
        }

        var action = await remediations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Remediation '{id}' was not found.");

        EnsureExecutable(action);

        var now = DateTimeOffset.UtcNow;
        action.Status = RemediationStatus.Executing;
        action.ApprovedAt = now;
        action.ApprovedBy = approvedBy.Trim();
        await remediations.SaveChangesAsync(cancellationToken);

        try
        {
            var outcome = await ExecuteAsync(action, cancellationToken);
            action.Status = outcome.Succeeded ? RemediationStatus.Completed : RemediationStatus.Failed;
            action.Result = Truncate(outcome.Message);
            action.FailureReason = outcome.Succeeded ? null : Truncate(outcome.Message);
            action.ExecutedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Remediation {RemediationId} failed during execution", action.Id);
            action.Status = RemediationStatus.Failed;
            action.FailureReason = Truncate(exception.Message);
            action.ExecutedAt = DateTimeOffset.UtcNow;
        }

        await remediations.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Remediation {RemediationId} finished with status {Status} approved by {ApprovedBy}",
            action.Id,
            action.Status,
            action.ApprovedBy);
        return ToResponse(action);
    }

    public async Task<RemediationResponse> RejectAsync(
        Guid id,
        string rejectedBy,
        RejectRemediationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rejectedBy))
        {
            throw new DomainValidationException("rejectedBy", "A rejector identity is required.");
        }

        var action = await remediations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Remediation '{id}' was not found.");

        if (action.Status != RemediationStatus.Recommended)
        {
            throw new ConflictException("Only a recommended remediation can be rejected.");
        }

        action.Status = RemediationStatus.Rejected;
        action.RejectedBy = rejectedBy.Trim();
        action.RejectionReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        await remediations.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Rejected remediation {RemediationId} by {RejectedBy}", action.Id, action.RejectedBy);
        return ToResponse(action);
    }

    private static void EnsureExecutable(RemediationAction action)
    {
        if (action.Status == RemediationStatus.Rejected)
        {
            throw new ForbiddenException("A rejected remediation cannot be executed.");
        }

        if (action.Status is RemediationStatus.Executing or RemediationStatus.Completed or RemediationStatus.Failed
            or RemediationStatus.Approved)
        {
            throw new ConflictException("This remediation has already been approved or executed.");
        }

        if (action.Status != RemediationStatus.Recommended)
        {
            throw new ConflictException("Only a recommended remediation can be approved.");
        }
    }

    private async Task<ContainerOperationResult> ExecuteAsync(
        RemediationAction action,
        CancellationToken cancellationToken)
    {
        var service = await services.GetByIdAsync(action.ServiceId, cancellationToken)
            ?? throw new NotFoundException($"Service '{action.ServiceId}' was not found.");

        return action.ActionType switch
        {
            RemediationActionType.RunHealthCheck => await RunHealthCheckAsync(service, cancellationToken),
            RemediationActionType.RefreshContainerStatus => await RefreshContainerAsync(service, cancellationToken),
            RemediationActionType.CollectRecentLogs => await CollectLogsAsync(service, cancellationToken),
            RemediationActionType.RestartContainer => await RestartAsync(service, cancellationToken),
            _ => new ContainerOperationResult(false, "Unsupported remediation type.")
        };
    }

    private async Task<ContainerOperationResult> RunHealthCheckAsync(
        MonitoredService service,
        CancellationToken cancellationToken)
    {
        await healthMonitor.CheckServiceAsync(service.Id, cancellationToken);
        var refreshed = await services.GetByIdAsync(service.Id, cancellationToken);
        return new ContainerOperationResult(
            true,
            $"Health check executed. Current application status is {refreshed?.Status}.");
    }

    private async Task<ContainerOperationResult> RefreshContainerAsync(
        MonitoredService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(service.ContainerName))
        {
            return new ContainerOperationResult(false, "Service has no associated container.");
        }

        var snapshot = await containers.GetByNameAsync(service.ContainerName, cancellationToken);
        if (snapshot is null)
        {
            return new ContainerOperationResult(false, "Docker did not return a snapshot for this container.");
        }

        service.ContainerId = snapshot.Id;
        service.ContainerImage = snapshot.Image;
        service.ContainerRunning = snapshot.Found && snapshot.Running;
        service.ContainerStartedAt = snapshot.StartedAt;
        service.ContainerRestartCount = snapshot.RestartCount;
        service.ContainerHealth = snapshot.Found ? snapshot.Health : ContainerHealth.NotFound;
        service.ContainerCpuPercent = snapshot.CpuPercent;
        service.ContainerMemoryBytes = snapshot.MemoryUsageBytes;
        service.ContainerObservedAt = DateTimeOffset.UtcNow;
        service.UpdatedAt = DateTimeOffset.UtcNow;
        await services.SaveChangesAsync(cancellationToken);
        return new ContainerOperationResult(
            true,
            $"Container '{service.ContainerName}' running={service.ContainerRunning} restarts={service.ContainerRestartCount} health={service.ContainerHealth}.");
    }

    private async Task<ContainerOperationResult> CollectLogsAsync(
        MonitoredService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(service.ContainerName))
        {
            return new ContainerOperationResult(false, "Service has no associated container.");
        }

        var logs = await containers.GetLogsAsync(service.ContainerName, 100, cancellationToken);
        return new ContainerOperationResult(true, logs);
    }

    private async Task<ContainerOperationResult> RestartAsync(
        MonitoredService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(service.ContainerName))
        {
            return new ContainerOperationResult(false, "Service has no associated container.");
        }

        var associated = service.ContainerName;
        return await containers.RestartByNameAsync(associated, cancellationToken);
    }

    private static string DefaultDescription(RemediationActionType type, MonitoredService service) =>
        type switch
        {
            RemediationActionType.RestartContainer =>
                $"Restart container '{service.ContainerName}' associated with {service.Name}. This has not been executed.",
            RemediationActionType.RunHealthCheck =>
                $"Run an on-demand health check for {service.Name}.",
            RemediationActionType.RefreshContainerStatus =>
                $"Refresh Docker inspect data for '{service.ContainerName}'.",
            RemediationActionType.CollectRecentLogs =>
                $"Collect the last 100 log lines from '{service.ContainerName}'.",
            _ => "Predefined remediation."
        };

    private static RemediationResponse ToResponse(RemediationAction action) =>
        new(
            action.Id,
            action.IncidentId,
            action.ServiceId,
            action.ActionType,
            action.Description,
            action.Status,
            action.RequestedAt,
            action.ApprovedAt,
            action.ExecutedAt,
            action.ApprovedBy,
            action.RejectedBy,
            action.RejectionReason,
            action.Result,
            action.FailureReason,
            action.CreatedAt);

    private static string Truncate(string value) =>
        value.Length <= 8000 ? value : value[..8000];
}
