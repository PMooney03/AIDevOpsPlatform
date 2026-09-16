using DevOps.Application.Abstractions;
using DevOps.Application.Common;
using DevOps.Application.Contracts;
using DevOps.Application.Mapping;
using DevOps.Application.Monitoring;
using Microsoft.Extensions.Options;

namespace DevOps.Application.Services;

public sealed class ServiceHealthQueryService(
    IMonitoredServiceRepository services,
    IHealthCheckResultRepository healthChecks,
    IOptions<MonitoringOptions> options)
{
    public async Task<HealthCheckResultResponse> GetLatestAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        if (!await services.ExistsAsync(serviceId, cancellationToken))
        {
            throw new NotFoundException($"Service '{serviceId}' was not found.");
        }

        var latest = await healthChecks.GetLatestAsync(serviceId, cancellationToken)
            ?? throw new NotFoundException($"No health checks have been recorded for service '{serviceId}'.");

        return latest.ToResponse();
    }

    public async Task<IReadOnlyList<HealthCheckResultResponse>> GetHistoryAsync(
        Guid serviceId,
        int? limit,
        CancellationToken cancellationToken)
    {
        if (!await services.ExistsAsync(serviceId, cancellationToken))
        {
            throw new NotFoundException($"Service '{serviceId}' was not found.");
        }

        var take = limit ?? options.Value.HistoryDefaultLimit;
        if (take < 1 || take > options.Value.HistoryMaxLimit)
        {
            throw new DomainValidationException(
                "limit",
                $"Limit must be between 1 and {options.Value.HistoryMaxLimit}.");
        }

        var history = await healthChecks.GetHistoryAsync(serviceId, take, cancellationToken);
        return history.Select(item => item.ToResponse()).ToArray();
    }

    public async Task<ServiceRuntimeResponse> GetRuntimeAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var service = await services.GetByIdAsync(serviceId, cancellationToken)
            ?? throw new NotFoundException($"Service '{serviceId}' was not found.");

        var latest = await healthChecks.GetLatestAsync(serviceId, cancellationToken);
        ContainerRuntimeResponse? container = string.IsNullOrWhiteSpace(service.ContainerName)
            ? null
            : service.ToContainerResponse();

        return new ServiceRuntimeResponse(
            service.Id,
            service.Name,
            service.Status,
            latest?.ToResponse(),
            container);
    }
}
