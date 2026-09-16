using DevOps.Application.Abstractions;
using DevOps.Application.Common;
using DevOps.Application.Contracts;
using DevOps.Application.Mapping;
using DevOps.Application.Validation;
using DevOps.Core.Entities;
using DevOps.Core.Enums;
using Microsoft.Extensions.Logging;

namespace DevOps.Application.Services;

public sealed class IncidentService(
    IIncidentRepository incidents,
    IMonitoredServiceRepository services,
    IPlatformMetrics metrics,
    ILogger<IncidentService> logger)
{
    public async Task<IReadOnlyList<IncidentResponse>> GetAllAsync(
        IncidentStatus? status,
        IncidentSeverity? severity,
        Guid? serviceId,
        CancellationToken cancellationToken)
    {
        var items = await incidents.GetAllAsync(cancellationToken);
        return items
            .Where(item => status is null || item.Status == status)
            .Where(item => severity is null || item.Severity == severity)
            .Where(item => serviceId is null || item.ServiceId == serviceId)
            .Select(item => item.ToResponse())
            .ToArray();
    }

    public async Task<IncidentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var incident = await incidents.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Incident '{id}' was not found.");

        return incident.ToResponse();
    }

    public async Task<IncidentResponse> CreateAsync(CreateIncidentRequest request, CancellationToken cancellationToken)
    {
        if (request.ServiceId == Guid.Empty)
        {
            throw new DomainValidationException("serviceId", "Service ID is required.");
        }

        IncidentValidator.Validate(request.Title, request.Description);

        if (!await services.ExistsAsync(request.ServiceId, cancellationToken))
        {
            throw new NotFoundException($"Service '{request.ServiceId}' was not found.");
        }

        if (!Enum.IsDefined(request.Severity))
        {
            throw new DomainValidationException("severity", "Severity is not a valid incident severity.");
        }

        var now = DateTimeOffset.UtcNow;
        var detectedAt = request.DetectedAt ?? now;
        var incident = new Incident
        {
            Id = Guid.CreateVersion7(),
            ServiceId = request.ServiceId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Severity = request.Severity,
            Status = IncidentStatus.Open,
            DetectedAt = detectedAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        await incidents.AddAsync(incident, cancellationToken);
        await incidents.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created incident {IncidentId} for service {ServiceId} with severity {Severity}",
            incident.Id,
            incident.ServiceId,
            incident.Severity);
        metrics.RecordIncidentCreated(incident.ServiceId, incident.Id, incident.Severity);

        return incident.ToResponse();
    }

    public async Task<IncidentResponse> UpdateAsync(
        Guid id,
        UpdateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        IncidentValidator.Validate(request.Title, request.Description);

        if (!Enum.IsDefined(request.Severity))
        {
            throw new DomainValidationException("severity", "Severity is not a valid incident severity.");
        }

        if (!Enum.IsDefined(request.Status))
        {
            throw new DomainValidationException("status", "Status is not a valid incident status.");
        }

        var incident = await incidents.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Incident '{id}' was not found.");

        var now = DateTimeOffset.UtcNow;
        incident.Title = request.Title.Trim();
        incident.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        incident.Severity = request.Severity;
        incident.Resolution = string.IsNullOrWhiteSpace(request.Resolution) ? null : request.Resolution.Trim();
        incident.ApplyStatus(request.Status, now);
        incident.UpdatedAt = now;

        await incidents.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated incident {IncidentId} to status {Status} and severity {Severity}",
            incident.Id,
            incident.Status,
            incident.Severity);

        return incident.ToResponse();
    }
}
