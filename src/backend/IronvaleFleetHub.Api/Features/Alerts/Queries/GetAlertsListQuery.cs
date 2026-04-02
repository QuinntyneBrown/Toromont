using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Alerts.Queries;

public record GetAlertsListQuery : IRequest<List<Alert>>;

public class GetAlertsListQueryHandler : IRequestHandler<GetAlertsListQuery, List<Alert>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetAlertsListQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<Alert>> Handle(GetAlertsListQuery request, CancellationToken ct)
    {
        return await _db.Alerts
            .Include(a => a.Equipment)
            .Where(a => a.OrganizationId == _tenant.OrganizationId && a.Status == "Active")
            .OrderBy(a => a.Severity == "Critical" ? 0 :
                          a.Severity == "High" ? 1 :
                          a.Severity == "Medium" ? 2 : 3)
            .ThenByDescending(a => a.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
