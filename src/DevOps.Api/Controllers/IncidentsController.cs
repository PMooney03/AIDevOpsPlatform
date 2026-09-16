using DevOps.Api.Auth;
using DevOps.Application.Contracts;
using DevOps.Application.Services;
using DevOps.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevOps.Api.Controllers;

[ApiController]
[Route("api/incidents")]
[Authorize(Policy = PlatformRoles.ViewerPolicy)]
public sealed class IncidentsController(IncidentService incidents) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IncidentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IncidentResponse>>> GetAll(
        [FromQuery] IncidentStatus? status,
        [FromQuery] IncidentSeverity? severity,
        [FromQuery] Guid? serviceId,
        CancellationToken cancellationToken)
    {
        return Ok(await incidents.GetAllAsync(status, severity, serviceId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IncidentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await incidents.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = PlatformRoles.OperatorPolicy)]
    [ProducesResponseType(typeof(IncidentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponse>> Create(
        [FromBody] CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await incidents.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PlatformRoles.OperatorPolicy)]
    [ProducesResponseType(typeof(IncidentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponse>> Update(
        Guid id,
        [FromBody] UpdateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await incidents.UpdateAsync(id, request, cancellationToken);
        return Ok(created);
    }

    [HttpPost("{id:guid}/analyze")]
    [Authorize(Policy = PlatformRoles.OperatorPolicy)]
    [ProducesResponseType(typeof(IncidentAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentAnalysisResponse>> Analyze(
        Guid id,
        [FromServices] IncidentAnalysisService analysis,
        CancellationToken cancellationToken)
    {
        return Ok(await analysis.AnalyzeAsync(id, cancellationToken));
    }

    [HttpGet("{id:guid}/analysis")]
    [ProducesResponseType(typeof(IncidentAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentAnalysisResponse>> GetAnalysis(
        Guid id,
        [FromServices] IncidentAnalysisService analysis,
        CancellationToken cancellationToken)
    {
        var latest = await analysis.GetLatestAsync(id, cancellationToken);
        return latest is null ? NoContent() : Ok(latest);
    }

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Policy = PlatformRoles.OperatorPolicy)]
    [ProducesResponseType(typeof(IncidentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponse>> Resolve(
        Guid id,
        [FromBody] ResolveIncidentRequest request,
        [FromServices] IncidentAnalysisService analysis,
        CancellationToken cancellationToken)
    {
        return Ok(await analysis.ResolveAsync(id, request, cancellationToken));
    }

    [HttpGet("{id:guid}/similar")]
    [ProducesResponseType(typeof(IReadOnlyList<SimilarIncidentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SimilarIncidentResponse>>> Similar(
        Guid id,
        [FromServices] IncidentAnalysisService analysis,
        CancellationToken cancellationToken)
    {
        return Ok(await analysis.GetSimilarAsync(id, cancellationToken));
    }
}
