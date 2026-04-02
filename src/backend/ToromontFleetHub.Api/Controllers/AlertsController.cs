using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/alerts")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(FleetHubDbContext db, ITenantContext tenant, ILogger<AlertsController> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Alert>>> GetAll(CancellationToken ct)
    {
        var alerts = await _db.Alerts
            .Include(a => a.Equipment)
            .Where(a => a.OrganizationId == _tenant.OrganizationId && a.Status == "Active")
            .OrderByDescending(a => a.Severity == "Critical" ? 0 :
                                     a.Severity == "High" ? 1 :
                                     a.Severity == "Medium" ? 2 : 3)
            .ThenByDescending(a => a.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(alerts);
    }

    [HttpPut("{id:guid}/acknowledge")]
    public async Task<ActionResult<Alert>> Acknowledge(Guid id, CancellationToken ct)
    {
        var alert = await _db.Alerts
            .FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == _tenant.OrganizationId, ct);

        if (alert is null)
            return NotFound();

        if (alert.Status != "Active")
            return BadRequest(new { Error = "Alert is not in active state." });

        alert.Status = "Acknowledged";
        alert.AcknowledgedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Alert {AlertId} acknowledged by user {UserId}", id, _tenant.UserId);

        return Ok(alert);
    }

    [HttpPut("{id:guid}/resolve")]
    public async Task<ActionResult<Alert>> Resolve(Guid id, CancellationToken ct)
    {
        var alert = await _db.Alerts
            .FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == _tenant.OrganizationId, ct);

        if (alert is null)
            return NotFound();

        if (alert.Status == "Resolved")
            return BadRequest(new { Error = "Alert is already resolved." });

        alert.Status = "Resolved";
        alert.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Alert {AlertId} resolved by user {UserId}", id, _tenant.UserId);

        return Ok(alert);
    }
}
