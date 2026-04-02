using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToromontFleetHub.Api.Features.Alerts.Commands;
using ToromontFleetHub.Api.Features.Alerts.Queries;
using ToromontFleetHub.Api.Models;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/alerts")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AlertsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<Alert>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAlertsListQuery(), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/acknowledge")]
    public async Task<ActionResult<Alert>> Acknowledge(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new AcknowledgeAlertCommand(id), ct);
        if (!result.IsSuccess) return result.Error == "Not found." ? NotFound() : BadRequest(new { Error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/resolve")]
    public async Task<ActionResult<Alert>> Resolve(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ResolveAlertCommand(id), ct);
        if (!result.IsSuccess) return result.Error == "Not found." ? NotFound() : BadRequest(new { Error = result.Error });
        return Ok(result.Value);
    }
}
