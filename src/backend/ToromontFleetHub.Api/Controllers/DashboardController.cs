using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(FleetHubDbContext db, ITenantContext tenant, ILogger<DashboardController> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<DashboardStats>> GetKpis(CancellationToken ct)
    {
        var orgId = _tenant.OrganizationId;

        var totalEquipment = await _db.Equipment
            .CountAsync(e => e.OrganizationId == orgId, ct);

        var operationalEquipment = await _db.Equipment
            .CountAsync(e => e.OrganizationId == orgId && e.Status == "Operational", ct);

        var needsServiceEquipment = await _db.Equipment
            .CountAsync(e => e.OrganizationId == orgId && e.Status == "NeedsService", ct);

        var outOfServiceEquipment = await _db.Equipment
            .CountAsync(e => e.OrganizationId == orgId && e.Status == "OutOfService", ct);

        var openWorkOrders = await _db.WorkOrders
            .CountAsync(w => w.OrganizationId == orgId
                && (w.Status == "Open" || w.Status == "InProgress"), ct);

        var criticalAlerts = await _db.Alerts
            .CountAsync(a => a.OrganizationId == orgId
                && a.Status == "Active"
                && a.Severity == "Critical", ct);

        var pendingOrders = await _db.PartsOrders
            .CountAsync(o => o.OrganizationId == orgId
                && o.Status == "Submitted", ct);

        var aiPredictions = await _db.AIPredictions
            .CountAsync(p => p.OrganizationId == orgId && !p.IsDismissed, ct);

        return Ok(new DashboardStats
        {
            TotalEquipment = totalEquipment,
            OperationalEquipment = operationalEquipment,
            NeedsServiceEquipment = needsServiceEquipment,
            OutOfServiceEquipment = outOfServiceEquipment,
            OpenWorkOrders = openWorkOrders,
            CriticalAlerts = criticalAlerts,
            PendingOrders = pendingOrders,
            AIPredictions = aiPredictions
        });
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<List<Alert>>> GetRecentAlerts(CancellationToken ct)
    {
        var alerts = await _db.Alerts
            .Include(a => a.Equipment)
            .Where(a => a.OrganizationId == _tenant.OrganizationId && a.Status == "Active")
            .OrderByDescending(a => a.Severity == "Critical" ? 0 :
                                     a.Severity == "High" ? 1 :
                                     a.Severity == "Medium" ? 2 : 3)
            .ThenByDescending(a => a.CreatedAt)
            .Take(10)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(alerts);
    }
}
