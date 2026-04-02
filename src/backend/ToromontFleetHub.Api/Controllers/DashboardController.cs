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

        var activeEquipment = await _db.Equipment
            .CountAsync(e => e.OrganizationId == orgId && e.Status == "Operational", ct);

        var serviceRequired = await _db.Equipment
            .CountAsync(e => e.OrganizationId == orgId
                && (e.Status == "NeedsService" || e.Status == "OutOfService"), ct);

        var overdueWorkOrders = await _db.WorkOrders
            .CountAsync(w => w.OrganizationId == orgId
                && (w.Status == "Open" || w.Status == "InProgress")
                && w.RequestedDate < DateTime.UtcNow, ct);

        var fleetUtilization = totalEquipment > 0
            ? Math.Round((double)activeEquipment / totalEquipment * 100, 1)
            : 0;

        return Ok(new DashboardStats
        {
            TotalEquipment = totalEquipment,
            ActiveEquipment = activeEquipment,
            ServiceRequired = serviceRequired,
            OverdueWorkOrders = overdueWorkOrders,
            FleetUtilization = fleetUtilization
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
