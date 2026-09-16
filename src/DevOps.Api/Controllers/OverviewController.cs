using DevOps.Api.Auth;
using DevOps.Application.Contracts;
using DevOps.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevOps.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Policy = PlatformRoles.ViewerPolicy)]
public sealed class OverviewController(OverviewService overview) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(OverviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OverviewResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await overview.GetAsync(cancellationToken));
    }
}
