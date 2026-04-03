using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.Features.Me.Commands;
using IronvaleFleetHub.Api.Features.Me.Queries;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator) => _mediator = mediator;

    [HttpGet("context")]
    public async Task<ActionResult<CurrentUserContextResponse>> GetContext(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCurrentUserContextQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("active-organization")]
    public async Task<ActionResult<CurrentUserContextResponse>> SetActiveOrganization(
        [FromBody] SetActiveOrganizationRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SetActiveOrganizationCommand(request.OrganizationId), ct);
        if (!result.IsSuccess) return Forbid();
        return Ok(result.Value);
    }
}

public class SetActiveOrganizationRequest
{
    public Guid OrganizationId { get; set; }
}
