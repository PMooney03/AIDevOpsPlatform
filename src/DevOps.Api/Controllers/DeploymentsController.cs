using DevOps.Api.Auth;
using DevOps.Application.Contracts;
using DevOps.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevOps.Api.Controllers;

[ApiController]
[Route("api/deployments")]
[Authorize(Policy = PlatformRoles.AdminPolicy)]
public sealed class DeploymentsController(DeploymentService deployments) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(DeploymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeploymentResponse>> Create(
        [FromBody] CreateDeploymentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await deployments.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = created.Id }, created);
    }
}

[ApiController]
[Route("api/services")]
[Authorize(Policy = PlatformRoles.ViewerPolicy)]
public sealed class ServiceDeploymentsController(DeploymentService deployments) : ControllerBase
{
    [HttpGet("{id:guid}/deployments")]
    [ProducesResponseType(typeof(IReadOnlyList<DeploymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DeploymentResponse>>> GetForService(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await deployments.GetForServiceAsync(id, cancellationToken));
    }
}
