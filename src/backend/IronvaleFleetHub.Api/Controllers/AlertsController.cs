using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Features.Alerts.Commands;
using IronvaleFleetHub.Api.Features.Alerts.Queries;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/alerts")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AlertsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAlertsListQuery(page, pageSize), ct);
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

    [HttpGet("thresholds")]
    public async Task<ActionResult> GetThresholds(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAlertThresholdsQuery(), ct);
        return Ok(result);
    }

    [HttpPost("thresholds")]
    [Authorize(Policy = "RequireAdminOrFleetManager")]
    public async Task<ActionResult> CreateThreshold([FromBody] CreateAlertThresholdCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess) return BadRequest(new { Error = result.Error });
        return CreatedAtAction(nameof(GetThresholds), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("thresholds/{id:guid}")]
    [Authorize(Policy = "RequireAdminOrFleetManager")]
    public async Task<ActionResult> UpdateThreshold(Guid id, [FromBody] UpdateAlertThresholdRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateAlertThresholdCommand(id, request.MetricName, request.WarningValue, request.CriticalValue, request.EquipmentId), ct);
        if (!result.IsSuccess) return result.Error == "Not found." ? NotFound() : BadRequest(new { Error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("thresholds/{id:guid}")]
    [Authorize(Policy = "RequireAdminOrFleetManager")]
    public async Task<ActionResult> DeleteThreshold(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteAlertThresholdCommand(id), ct);
        if (!result.IsSuccess) return result.Error == "Not found." ? NotFound() : BadRequest(new { Error = result.Error });
        return NoContent();
    }
}
