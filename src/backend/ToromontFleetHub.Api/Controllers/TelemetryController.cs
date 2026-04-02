using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/telemetry")]
public class TelemetryController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAlertEvaluatorService _alertEvaluator;
    private readonly ILogger<TelemetryController> _logger;

    public TelemetryController(
        FleetHubDbContext db,
        ITenantContext tenant,
        IAlertEvaluatorService alertEvaluator,
        ILogger<TelemetryController> logger)
    {
        _db = db;
        _tenant = tenant;
        _alertEvaluator = alertEvaluator;
        _logger = logger;
    }

    [HttpPost("ingest")]
    [AllowAnonymous]
    public async Task<ActionResult> Ingest(
        [FromHeader(Name = "X-Api-Key")] string apiKey,
        [FromBody] TelemetryIngestionRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { Error = "API key is required." });

        // Validate API key against configuration
        var configuredKey = Environment.GetEnvironmentVariable("TELEMETRY_API_KEY") ?? "default-telemetry-key";
        if (apiKey != configuredKey)
            return Unauthorized(new { Error = "Invalid API key." });

        var equipment = await _db.Equipment
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EquipmentId, ct);

        if (equipment is null)
            return BadRequest(new { Error = "Equipment not found." });

        var telemetryEvent = new TelemetryEvent
        {
            Id = Guid.NewGuid(),
            EquipmentId = request.EquipmentId,
            EventType = request.EventType,
            Timestamp = DateTime.UtcNow,
            EngineHours = request.EngineHours,
            FuelLevel = request.FuelLevel,
            Temperature = request.Temperature,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Payload = request.Payload
        };

        _db.TelemetryEvents.Add(telemetryEvent);
        await _db.SaveChangesAsync(ct);

        // Evaluate thresholds and create alerts if needed
        await _alertEvaluator.EvaluateAsync(telemetryEvent);

        return Accepted(new { Id = telemetryEvent.Id });
    }

    [HttpGet("{equipmentId:guid}/metrics")]
    [Authorize]
    public async Task<ActionResult> GetMetrics(
        Guid equipmentId,
        [FromQuery] string range = "24h",
        CancellationToken ct = default)
    {
        var orgId = _tenant.OrganizationId;
        var equipmentExists = await _db.Equipment
            .AnyAsync(e => e.Id == equipmentId && e.OrganizationId == orgId, ct);

        if (!equipmentExists)
            return NotFound();

        var since = range.ToLowerInvariant() switch
        {
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            "90d" => DateTime.UtcNow.AddDays(-90),
            _ => DateTime.UtcNow.AddHours(-24)
        };

        var metrics = await _db.TelemetryEvents
            .Where(t => t.EquipmentId == equipmentId && t.Timestamp >= since)
            .OrderBy(t => t.Timestamp)
            .AsNoTracking()
            .Select(t => new
            {
                t.Timestamp,
                t.EngineHours,
                t.FuelLevel,
                t.Temperature
            })
            .ToListAsync(ct);

        return Ok(metrics);
    }

    [HttpGet("{equipmentId:guid}/latest")]
    [Authorize]
    public async Task<ActionResult> GetLatest(Guid equipmentId, CancellationToken ct)
    {
        var orgId = _tenant.OrganizationId;
        var equipmentExists = await _db.Equipment
            .AnyAsync(e => e.Id == equipmentId && e.OrganizationId == orgId, ct);

        if (!equipmentExists)
            return NotFound();

        var latest = await _db.TelemetryEvents
            .Where(t => t.EquipmentId == equipmentId)
            .OrderByDescending(t => t.Timestamp)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (latest is null)
            return Ok(new { });

        return Ok(latest);
    }

    [HttpGet("{equipmentId:guid}/gps-trail")]
    [Authorize]
    public async Task<ActionResult> GetGpsTrail(
        Guid equipmentId,
        [FromQuery] string range = "24h",
        CancellationToken ct = default)
    {
        var orgId = _tenant.OrganizationId;
        var equipmentExists = await _db.Equipment
            .AnyAsync(e => e.Id == equipmentId && e.OrganizationId == orgId, ct);

        if (!equipmentExists)
            return NotFound();

        var since = range.ToLowerInvariant() switch
        {
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            "90d" => DateTime.UtcNow.AddDays(-90),
            _ => DateTime.UtcNow.AddHours(-24)
        };

        var trail = await _db.TelemetryEvents
            .Where(t => t.EquipmentId == equipmentId && t.Timestamp >= since)
            .OrderBy(t => t.Timestamp)
            .AsNoTracking()
            .Select(t => new
            {
                t.Timestamp,
                t.Latitude,
                t.Longitude
            })
            .ToListAsync(ct);

        return Ok(trail);
    }
}
