using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Dashboard.Queries;

public record GetDashboardAlertsQuery : IRequest<List<Alert>>;

public class GetDashboardAlertsQueryHandler : IRequestHandler<GetDashboardAlertsQuery, List<Alert>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetDashboardAlertsQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<Alert>> Handle(GetDashboardAlertsQuery request, CancellationToken ct)
    {
        return await _db.Alerts
            .Include(a => a.Equipment)
            .Where(a => a.OrganizationId == _tenant.OrganizationId && a.Status == "Active")
            .OrderBy(a => a.Severity == "Critical" ? 0 :
                          a.Severity == "High" ? 1 :
                          a.Severity == "Medium" ? 2 : 3)
            .ThenByDescending(a => a.CreatedAt)
            .Take(10)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
