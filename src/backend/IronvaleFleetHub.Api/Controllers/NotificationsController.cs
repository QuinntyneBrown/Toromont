using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Features.Notifications.Commands;
using IronvaleFleetHub.Api.Features.Notifications.Queries;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<Notification>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetNotificationsQuery(skip, take), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<Notification>> MarkAsRead(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand(id), ct);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        await _mediator.Send(new MarkAllNotificationsReadCommand(), ct);
        return NoContent();
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult> GetUnreadCount(CancellationToken ct)
    {
        var count = await _mediator.Send(new GetUnreadCountQuery(), ct);
        return Ok(new { UnreadCount = count });
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<List<NotificationPreference>>> GetPreferences(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetNotificationPreferencesQuery(), ct);
        return Ok(result);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<List<NotificationPreference>>> UpdatePreferences(
        [FromBody] List<NotificationPreference> preferences,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateNotificationPreferencesCommand(preferences), ct);
        return Ok(result);
    }
}
