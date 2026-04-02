using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.AIInsights.Queries;

public record GetAnomaliesQuery : IRequest<List<AnomalyDetection>>;

public class GetAnomaliesQueryHandler : IRequestHandler<GetAnomaliesQuery, List<AnomalyDetection>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetAnomaliesQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<AnomalyDetection>> Handle(GetAnomaliesQuery request, CancellationToken ct)
    {
        return await _db.AnomalyDetections
            .Include(a => a.Equipment)
            .Where(a => a.OrganizationId == _tenant.OrganizationId)
            .OrderByDescending(a => a.DetectedAt)
            .Take(100)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
