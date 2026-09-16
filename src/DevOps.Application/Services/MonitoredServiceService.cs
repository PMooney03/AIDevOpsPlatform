using DevOps.Application.Abstractions;
using DevOps.Application.Common;
using DevOps.Application.Contracts;
using DevOps.Application.Mapping;
using DevOps.Application.Validation;
using DevOps.Core.Entities;
using DevOps.Core.Enums;
using Microsoft.Extensions.Logging;

namespace DevOps.Application.Services;

public sealed class MonitoredServiceService(
    IMonitoredServiceRepository services,
    IIncidentRepository incidents,
    ILogger<MonitoredServiceService> logger)
{
    public async Task<IReadOnlyList<MonitoredServiceResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await services.GetAllAsync(cancellationToken);
        return items.Select(item => item.ToResponse()).ToArray();
    }

    public async Task<MonitoredServiceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await services.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Service '{id}' was not found.");

        return service.ToResponse();
    }

    public async Task<MonitoredServiceResponse> CreateAsync(
        CreateMonitoredServiceRequest request,
        CancellationToken cancellationToken)
    {
        ServiceRegistrationValidator.Validate(
            request.Name,
            request.Description,
            request.BaseUrl,
            request.HealthEndpoint,
            request.ContainerName);

        var name = request.Name.Trim();
        if (await services.GetByNameAsync(name, cancellationToken) is not null)
        {
            throw new ConflictException($"A service named '{name}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var service = new MonitoredService
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            BaseUrl = request.BaseUrl.Trim().TrimEnd('/'),
            HealthEndpoint = request.HealthEndpoint.Trim(),
            Status = ServiceStatus.Unknown,
            MonitoringEnabled = request.MonitoringEnabled,
            ContainerName = string.IsNullOrWhiteSpace(request.ContainerName) ? null : request.ContainerName.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await services.AddAsync(service, cancellationToken);
        await services.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Registered monitored service {ServiceId} named {ServiceName}", service.Id, service.Name);
        return service.ToResponse();
    }

    public async Task<MonitoredServiceResponse> UpdateAsync(
        Guid id,
        UpdateMonitoredServiceRequest request,
        CancellationToken cancellationToken)
    {
        ServiceRegistrationValidator.Validate(
            request.Name,
            request.Description,
            request.BaseUrl,
            request.HealthEndpoint,
            request.ContainerName);

        var service = await services.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Service '{id}' was not found.");

        var name = request.Name.Trim();
        var existingWithName = await services.GetByNameAsync(name, cancellationToken);
        if (existingWithName is not null && existingWithName.Id != id)
        {
            throw new ConflictException($"A service named '{name}' already exists.");
        }

        service.Name = name;
        service.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        service.BaseUrl = request.BaseUrl.Trim().TrimEnd('/');
        service.HealthEndpoint = request.HealthEndpoint.Trim();
        service.MonitoringEnabled = request.MonitoringEnabled;
        service.ContainerName = string.IsNullOrWhiteSpace(request.ContainerName) ? null : request.ContainerName.Trim();
        service.UpdatedAt = DateTimeOffset.UtcNow;

        await services.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated monitored service {ServiceId}", service.Id);
        return service.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await services.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Service '{id}' was not found.");

        if (await incidents.HasAnyForServiceAsync(id, cancellationToken))
        {
            throw new ConflictException(
                $"Service '{id}' cannot be deleted while incident history exists.");
        }

        services.Remove(service);
        await services.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted monitored service {ServiceId}", id);
    }
}
