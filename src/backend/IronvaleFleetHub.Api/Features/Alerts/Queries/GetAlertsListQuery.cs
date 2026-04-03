using IronvaleFleetHub.Api.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Alerts.Queries;

public record AlertListItem(
    Guid Id,
    string? EquipmentName,
    string AlertType,
    string Severity,
    string Message,
    string Status,
    DateTime CreatedAt);

public record PaginatedAlertsResponse(
    List<AlertListItem> Data,
    PaginationMeta Pagination);

public record PaginationMeta(int Page, int PageSize, int TotalCount);

public record GetAlertsListQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedAlertsResponse>, ISkipValidation;

public class GetAlertsListQueryHandler : IRequestHandler<GetAlertsListQuery, PaginatedAlertsResponse>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetAlertsListQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedAlertsResponse> Handle(GetAlertsListQuery request, CancellationToken ct)
    {
        var query = _db.Alerts
            .Include(a => a.Equipment)
            .Where(a => a.OrganizationId == _tenant.OrganizationId && a.Status == "Active")
            .OrderBy(a => a.Severity == "Critical" ? 0 :
                          a.Severity == "High" ? 1 :
                          a.Severity == "Medium" ? 2 : 3)
            .ThenByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .Select(a => new AlertListItem(
                a.Id,
                a.Equipment != null ? a.Equipment.Name : null,
                a.AlertType,
                a.Severity,
                a.Message,
                a.Status,
                a.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedAlertsResponse(items, new PaginationMeta(page, pageSize, totalCount));
    }
}
