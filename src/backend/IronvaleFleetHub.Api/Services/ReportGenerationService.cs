using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.DTOs;

namespace IronvaleFleetHub.Api.Services;

public class ReportGenerationService : IReportGenerationService
{
    private readonly FleetHubDbContext _db;

    public ReportGenerationService(FleetHubDbContext db)
    {
        _db = db;
    }

    public async Task<ReportResponse> GenerateFleetUtilizationAsync(ReportRequest request, Guid organizationId, CancellationToken ct = default)
    {
        var equipment = await _db.Equipment
            .IgnoreQueryFilters()
            .Where(e => e.OrganizationId == organizationId)
            .AsNoTracking()
            .ToListAsync(ct);

        if (request.EquipmentIds is { Count: > 0 })
            equipment = equipment.Where(e => request.EquipmentIds.Contains(e.Id)).ToList();

        var dataPoints = equipment.Select(e => new ReportDataPoint
        {
            Label = e.Name,
            Value = e.Status == "Operational" ? 1.0 : 0.0,
            Category = e.Category,
        }).ToList();

        return new ReportResponse
        {
            ReportTitle = "Fleet Utilization Report",
            GeneratedAt = DateTime.UtcNow,
            Summary = new ReportSummary
            {
                TotalEquipment = equipment.Count,
                ActiveWorkOrders = await _db.WorkOrders.IgnoreQueryFilters()
                    .CountAsync(w => w.OrganizationId == organizationId && w.Status != "Completed" && w.Status != "Closed", ct),
                CompletedWorkOrders = await _db.WorkOrders.IgnoreQueryFilters()
                    .CountAsync(w => w.OrganizationId == organizationId && w.Status == "Completed", ct),
            },
            DataPoints = dataPoints,
        };
    }

    public async Task<ReportResponse> GenerateMaintenanceCostsAsync(ReportRequest request, Guid organizationId, CancellationToken ct = default)
    {
        var orders = await _db.PartsOrders
            .IgnoreQueryFilters()
            .Where(o => o.OrganizationId == organizationId && o.CreatedAt >= request.StartDate && o.CreatedAt <= request.EndDate)
            .Include(o => o.LineItems)
            .AsNoTracking()
            .ToListAsync(ct);

        var dataPoints = orders.Select(o => new ReportDataPoint
        {
            Label = o.OrderNumber,
            Value = (double)o.Subtotal,
            Date = o.CreatedAt,
        }).ToList();

        return new ReportResponse
        {
            ReportTitle = "Maintenance Costs Report",
            GeneratedAt = DateTime.UtcNow,
            Summary = new ReportSummary
            {
                TotalPartsCost = orders.Sum(o => o.Subtotal),
            },
            DataPoints = dataPoints,
        };
    }
}
