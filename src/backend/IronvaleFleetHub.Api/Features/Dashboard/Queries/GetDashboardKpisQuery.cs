using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Dashboard.Queries;

public record GetDashboardKpisQuery : IRequest<DashboardStats>;

public class GetDashboardKpisQueryHandler : IRequestHandler<GetDashboardKpisQuery, DashboardStats>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetDashboardKpisQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<DashboardStats> Handle(GetDashboardKpisQuery request, CancellationToken ct)
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

        return new DashboardStats
        {
            TotalEquipment = totalEquipment,
            ActiveEquipment = activeEquipment,
            ServiceRequired = serviceRequired,
            OverdueWorkOrders = overdueWorkOrders,
            FleetUtilization = fleetUtilization
        };
    }
}
