using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Features.Telemetry.Commands;
using ToromontFleetHub.Api.Features.Telemetry.Queries;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/telemetry")]
public class TelemetryController : ControllerBase
{
    private readonly IMediator _mediator;

    public TelemetryController(IMediator mediator) => _mediator = mediator;

    [HttpPost("ingest")]
    [AllowAnonymous]
    public async Task<ActionResult> Ingest(
        [FromHeader(Name = "X-Api-Key")] string apiKey,
        [FromBody] TelemetryIngestionRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { Error = "API key is required." });

        var configuredKey = Environment.GetEnvironmentVariable("TELEMETRY_API_KEY") ?? "default-telemetry-key";
        if (apiKey != configuredKey)
            return Unauthorized(new { Error = "Invalid API key." });

        var result = await _mediator.Send(new IngestTelemetryCommand(
            request.EquipmentId,
            request.EventType,
            request.EngineHours,
            request.FuelLevel,
            request.Temperature,
            request.Latitude,
            request.Longitude,
            request.Payload), ct);

        if (!result.IsSuccess) return BadRequest(new { Error = result.Error });
        return Accepted(new { Id = result.Value });
    }

    [HttpGet("{equipmentId:guid}/metrics")]
    [Authorize]
    public async Task<ActionResult> GetMetrics(
        Guid equipmentId,
        [FromQuery] string range = "24h",
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetEquipmentMetricsQuery(equipmentId, range), ct);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }

    [HttpGet("{equipmentId:guid}/latest")]
    [Authorize]
    public async Task<ActionResult> GetLatest(Guid equipmentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLatestTelemetryQuery(equipmentId), ct);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value ?? (object)new { });
    }

    [HttpGet("{equipmentId:guid}/gps-trail")]
    [Authorize]
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
