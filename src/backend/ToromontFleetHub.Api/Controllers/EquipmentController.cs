using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/equipment")]
[Authorize]
public class EquipmentController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<EquipmentController> _logger;

    public EquipmentController(FleetHubDbContext db, ITenantContext tenant, ILogger<EquipmentController> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<Equipment>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? filter = null,
        CancellationToken ct = default)
    {
        var query = _db.Equipment
            .Where(e => e.OrganizationId == _tenant.OrganizationId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e =>
                e.Name.Contains(search) ||
                e.SerialNumber.Contains(search) ||
                e.Make.Contains(search) ||
                e.Model.Contains(search));

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var desc = sort.StartsWith("-");
            var field = desc ? sort[1..] : sort;
            query = field.ToLowerInvariant() switch
            {
                "name" => desc ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
                "status" => desc ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
                "createdat" => desc ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt),
                _ => query.OrderByDescending(e => e.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(e => e.CreatedAt);
        }

        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(take).ToListAsync(ct);

        return Ok(new PaginatedResponse<Equipment>
        {
            Items = items,
            Pagination = new PaginationInfo
            {
                Page = skip / take + 1,
                PageSize = take,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)take)
            }
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Equipment>> GetById(Guid id, CancellationToken ct)
    {
        var equipment = await _db.Equipment
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.OrganizationId == _tenant.OrganizationId, ct);

        if (equipment is null)
            return NotFound();

        return Ok(equipment);
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdminOrFleetManager")]
    public async Task<ActionResult<Equipment>> Create([FromBody] CreateEquipmentRequest request, CancellationToken ct)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(request.Make)) errors.Add("Make is required.");
        if (string.IsNullOrWhiteSpace(request.Model)) errors.Add("Model is required.");
        if (request.Year < 1900 || request.Year > DateTime.UtcNow.Year + 2) errors.Add("Year must be valid.");
        if (string.IsNullOrWhiteSpace(request.SerialNumber)) errors.Add("Serial number is required.");
        if (string.IsNullOrWhiteSpace(request.Category)) errors.Add("Category is required.");
        if (errors.Count > 0)
            return BadRequest(new { Errors = errors });

        var exists = await _db.Equipment.AnyAsync(
            e => e.SerialNumber == request.SerialNumber && e.OrganizationId == _tenant.OrganizationId, ct);

        if (exists)
            return BadRequest(new { Error = "Equipment with this serial number already exists in this organization." });

        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _tenant.OrganizationId,
            Name = request.Name,
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            SerialNumber = request.SerialNumber,
            Category = request.Category,
            Location = request.Location,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            GpsDeviceId = request.GpsDeviceId,
            PurchaseDate = request.PurchaseDate,
            WarrantyExpiration = request.WarrantyExpiration,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Equipment.Add(equipment);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Equipment {EquipmentId} created by user {UserId}", equipment.Id, _tenant.UserId);
        return CreatedAtAction(nameof(GetById), new { id = equipment.Id }, equipment);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Equipment>> Update(Guid id, [FromBody] UpdateEquipmentRequest request, CancellationToken ct)
    {
        var equipment = await _db.Equipment
            .FirstOrDefaultAsync(e => e.Id == id && e.OrganizationId == _tenant.OrganizationId, ct);

        if (equipment is null)
            return NotFound();

        if (request.Name is not null) equipment.Name = request.Name;
        if (request.Status is not null) equipment.Status = request.Status;
        if (request.Location is not null) equipment.Location = request.Location;
        if (request.Latitude.HasValue) equipment.Latitude = request.Latitude.Value;
        if (request.Longitude.HasValue) equipment.Longitude = request.Longitude.Value;
        if (request.GpsDeviceId is not null) equipment.GpsDeviceId = request.GpsDeviceId;
        if (request.Notes is not null) equipment.Notes = request.Notes;
        equipment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(equipment);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireAdminOrFleetManager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var equipment = await _db.Equipment
            .FirstOrDefaultAsync(e => e.Id == id && e.OrganizationId == _tenant.OrganizationId, ct);

        if (equipment is null)
            return NotFound();

        // Cascade delete related data
        var telemetry = _db.TelemetryEvents.Where(t => t.EquipmentId == id);
        var alerts = _db.Alerts.Where(a => a.EquipmentId == id);
        var predictions = _db.AIPredictions.Where(p => p.EquipmentId == id);
        var anomalies = _db.AnomalyDetections.Where(a => a.EquipmentId == id);
        var workOrders = _db.WorkOrders.Where(w => w.EquipmentId == id);

        _db.TelemetryEvents.RemoveRange(telemetry);
        _db.Alerts.RemoveRange(alerts);
        _db.AIPredictions.RemoveRange(predictions);
        _db.AnomalyDetections.RemoveRange(anomalies);

        foreach (var wo in await workOrders.Include(w => w.History).ToListAsync(ct))
        {
            _db.WorkOrderHistories.RemoveRange(wo.History);
            _db.WorkOrders.Remove(wo);
        }

        _db.Equipment.Remove(equipment);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Equipment {EquipmentId} deleted by user {UserId}", id, _tenant.UserId);
        return NoContent();
    }

    [HttpPost("import")]
    [Authorize(Policy = "RequireAdminOrFleetManager")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { Error = "No file uploaded." });

        if (!file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { Error = "Only XML files are accepted." });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { Error = "File exceeds maximum size of 10MB." });

        using var stream = file.OpenReadStream();
        var doc = await System.Xml.Linq.XDocument.LoadAsync(stream, System.Xml.Linq.LoadOptions.None, ct);

        var upsertCount = 0;
        var elements = doc.Descendants("Equipment");

        foreach (var el in elements)
        {
            var serial = el.Element("SerialNumber")?.Value;
            if (string.IsNullOrWhiteSpace(serial)) continue;

            var existing = await _db.Equipment
                .FirstOrDefaultAsync(e => e.SerialNumber == serial && e.OrganizationId == _tenant.OrganizationId, ct);

            if (existing is not null)
            {
                existing.Name = el.Element("Name")?.Value ?? existing.Name;
                existing.Make = el.Element("Make")?.Value ?? existing.Make;
                existing.Model = el.Element("Model")?.Value ?? existing.Model;
                if (int.TryParse(el.Element("Year")?.Value, out var year)) existing.Year = year;
                existing.Category = el.Element("Category")?.Value ?? existing.Category;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.Equipment.Add(new Equipment
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _tenant.OrganizationId,
                    SerialNumber = serial,
                    Name = el.Element("Name")?.Value ?? string.Empty,
                    Make = el.Element("Make")?.Value ?? string.Empty,
                    Model = el.Element("Model")?.Value ?? string.Empty,
                    Year = int.TryParse(el.Element("Year")?.Value, out var y) ? y : 0,
                    Category = el.Element("Category")?.Value ?? string.Empty,
                    Location = el.Element("Location")?.Value ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            upsertCount++;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Imported {Count} equipment records for org {OrgId}", upsertCount, _tenant.OrganizationId);

        return Ok(new { Imported = upsertCount });
    }
}
