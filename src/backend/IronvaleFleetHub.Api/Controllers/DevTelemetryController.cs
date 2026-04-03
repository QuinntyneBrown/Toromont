using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/dev/telemetry")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class DevTelemetryController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DevTelemetryController> _logger;

    public DevTelemetryController(
        FleetHubDbContext db,
        ITenantContext tenant,
        IHostEnvironment env,
        ILogger<DevTelemetryController> logger)
    {
        _db = db;
        _tenant = tenant;
        _env = env;
        _logger = logger;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] DevTelemetryIngestRequest request, CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        if (request.EquipmentId == Guid.Empty)
            return BadRequest(new { error = "EquipmentId is required." });

        if (string.IsNullOrEmpty(request.EventType))
            return BadRequest(new { error = "EventType is required." });

        var equipment = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(_db.Equipment, e => e.Id == request.EquipmentId, ct);

        if (equipment == null)
            return BadRequest(new { error = $"Equipment {request.EquipmentId} not found." });

        var telemetryEvent = new TelemetryEvent
        {
            Id = Guid.NewGuid(),
            EquipmentId = request.EquipmentId,
            OrganizationId = equipment.OrganizationId,
            EventType = request.EventType,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            EngineHours = request.EngineHours ?? 0,
            FuelLevel = request.FuelLevel ?? 0,
            Temperature = request.Temperature ?? 0,
            Latitude = request.Latitude ?? equipment.Latitude,
            Longitude = request.Longitude ?? equipment.Longitude
        };

        _db.TelemetryEvents.Add(telemetryEvent);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Dev telemetry ingested for equipment {EquipmentId}: {EventType}",
            request.EquipmentId, request.EventType);

        return Accepted(new { telemetryEvent.Id, telemetryEvent.Timestamp });
    }
}

public class DevTelemetryIngestRequest
{
    public Guid EquipmentId { get; set; }
    public string EventType { get; set; } = "Reading";
    public DateTime? Timestamp { get; set; }
    public double? EngineHours { get; set; }
    public double? FuelLevel { get; set; }
    public double? Temperature { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
