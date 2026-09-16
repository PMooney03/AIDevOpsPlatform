using System.Security.Claims;
using DevOps.Api.Auth;
using DevOps.Application.Contracts;
using DevOps.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevOps.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Policy = PlatformRoles.ViewerPolicy)]
public sealed class RemediationsController(RemediationService remediations) : ControllerBase
{
    [HttpPost("incidents/{incidentId:guid}/remediations")]
    [Authorize(Policy = PlatformRoles.OperatorPolicy)]
    [ProducesResponseType(typeof(RemediationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RemediationResponse>> Propose(
        Guid incidentId,
        [FromBody] CreateRemediationRequest request,
        CancellationToken cancellationToken)
    {
        var created = await remediations.ProposeAsync(incidentId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("incidents/{incidentId:guid}/remediations")]
    [ProducesResponseType(typeof(IReadOnlyList<RemediationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RemediationResponse>>> GetForIncident(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        return Ok(await remediations.GetForIncidentAsync(incidentId, cancellationToken));
    }

    [HttpGet("remediations/{id:guid}")]
    [ProducesResponseType(typeof(RemediationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RemediationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await remediations.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost("remediations/{id:guid}/approve")]
    [Authorize(Policy = PlatformRoles.OperatorPolicy)]
    [ProducesResponseType(typeof(RemediationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RemediationResponse>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var actor = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
        return Ok(await remediations.ApproveAsync(id, actor, cancellationToken));
    }

    [HttpPost("remediations/{id:guid}/reject")]
    [Authorize(Policy = PlatformRoles.OperatorPolicy)]
    [ProducesResponseType(typeof(RemediationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RemediationResponse>> Reject(
        Guid id,
        [FromBody] RejectRemediationRequest? request,
        CancellationToken cancellationToken)
    {
        var actor = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
        return Ok(await remediations.RejectAsync(id, actor, request ?? new RejectRemediationRequest(null), cancellationToken));
    }
}
