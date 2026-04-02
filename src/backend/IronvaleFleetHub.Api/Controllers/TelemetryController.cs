using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.Features.Telemetry.Queries;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/telemetry")]
[Authorize]
public class TelemetryController : ControllerBase
{
    private readonly IMediator _mediator;

    public TelemetryController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{equipmentId:guid}/metrics")]
    public async Task<ActionResult> GetMetrics(
        Guid equipmentId,
        [FromQuery] string range = "24h",
        [FromQuery] string? metrics = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetEquipmentMetricsQuery(equipmentId, range, metrics), ct);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }

    [HttpGet("{equipmentId:guid}/latest")]
    public async Task<ActionResult> GetLatest(Guid equipmentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLatestTelemetryQuery(equipmentId), ct);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value ?? (object)new { });
    }

    [HttpGet("{equipmentId:guid}/gps-trail")]
    public async Task<ActionResult> GetGpsTrail(
        Guid equipmentId,
        [FromQuery] string range = "24h",
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetGpsTrailQuery(equipmentId, range), ct);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }
}
