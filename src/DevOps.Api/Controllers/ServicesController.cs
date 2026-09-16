using DevOps.Api.Auth;
using DevOps.Application.Contracts;
using DevOps.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevOps.Api.Controllers;

[ApiController]
[Route("api/services")]
[Authorize(Policy = PlatformRoles.ViewerPolicy)]
public sealed class ServicesController(MonitoredServiceService services) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MonitoredServiceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MonitoredServiceResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await services.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MonitoredServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MonitoredServiceResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await services.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = PlatformRoles.AdminPolicy)]
    [ProducesResponseType(typeof(MonitoredServiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MonitoredServiceResponse>> Create(
        [FromBody] CreateMonitoredServiceRequest request,
        CancellationToken cancellationToken)
    {
        var created = await services.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PlatformRoles.AdminPolicy)]
    [ProducesResponseType(typeof(MonitoredServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MonitoredServiceResponse>> Update(
        Guid id,
        [FromBody] UpdateMonitoredServiceRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await services.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PlatformRoles.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await services.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/health")]
    [ProducesResponseType(typeof(HealthCheckResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HealthCheckResultResponse>> GetLatestHealth(
        Guid id,
        [FromServices] ServiceHealthQueryService health,
        CancellationToken cancellationToken)
    {
        return Ok(await health.GetLatestAsync(id, cancellationToken));
    }

    [HttpGet("{id:guid}/health/history")]
    [ProducesResponseType(typeof(IReadOnlyList<HealthCheckResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<HealthCheckResultResponse>>> GetHealthHistory(
        Guid id,
        [FromServices] ServiceHealthQueryService health,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        return Ok(await health.GetHistoryAsync(id, limit, cancellationToken));
    }

    [HttpGet("{id:guid}/runtime")]
    [ProducesResponseType(typeof(ServiceRuntimeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRuntimeResponse>> GetRuntime(
        Guid id,
        [FromServices] ServiceHealthQueryService health,
        CancellationToken cancellationToken)
    {
        return Ok(await health.GetRuntimeAsync(id, cancellationToken));
    }
}
