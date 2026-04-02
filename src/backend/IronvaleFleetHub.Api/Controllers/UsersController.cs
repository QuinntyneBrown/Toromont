using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Features.Users.Commands;
using IronvaleFleetHub.Api.Features.Users.Queries;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "RequireAdmin")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUsersListQuery(), ct);
        return Ok(result);
    }

    [HttpGet("assignable")]
    [Authorize(Policy = "RequireWrite")]
    public async Task<ActionResult<List<User>>> GetAssignable(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAssignableUsersQuery(), ct);
        return Ok(result);
    }

    [HttpPost("invite")]
    public async Task<ActionResult<UserInvitation>> InviteUser(
        [FromBody] InviteUserRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new InviteUserCommand(request.Email, request.Role), ct);
        if (!result.IsSuccess) return BadRequest(new { Error = result.Error });
        return CreatedAtAction(nameof(GetAll), result.Value);
    }

    [HttpPost("accept-invite")]
    [AllowAnonymous]
    public async Task<ActionResult<User>> AcceptInvite(
        [FromBody] AcceptInviteRequest request,
        CancellationToken ct)
    {
        var entraObjectId = User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                            ?? User.FindFirstValue("oid")
                            ?? string.Empty;
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        var result = await _mediator.Send(
            new AcceptInviteCommand(request.Token, entraObjectId, email, displayName), ct);

        if (!result.IsSuccess) return BadRequest(new { Error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<User>> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new ChangeUserRoleCommand(id, request.Role), ct);
        if (!result.IsSuccess) return result.Error == "Not found." ? NotFound() : BadRequest(new { Error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/deactivate")]
    public async Task<ActionResult<User>> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeactivateUserCommand(id), ct);
        if (!result.IsSuccess) return result.Error == "Not found." ? NotFound() : BadRequest(new { Error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<ActionResult<User>> Activate(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ActivateUserCommand(id), ct);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }
}
