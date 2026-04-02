using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.AIInsights.Queries;

public record AIDashboardStats(int TotalPredictions, int HighPriority, int AnomalyCount, decimal EstimatedSavings);

public record GetAIDashboardStatsQuery : IRequest<AIDashboardStats>;

public class GetAIDashboardStatsQueryHandler : IRequestHandler<GetAIDashboardStatsQuery, AIDashboardStats>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetAIDashboardStatsQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AIDashboardStats> Handle(GetAIDashboardStatsQuery request, CancellationToken ct)
    {
        var orgId = _tenant.OrganizationId;

        var totalPredictions = await _db.AIPredictions
            .CountAsync(p => p.OrganizationId == orgId && !p.IsDismissed, ct);

        var highPriority = await _db.AIPredictions
            .CountAsync(p => p.OrganizationId == orgId && !p.IsDismissed
                && (p.Priority == "Critical" || p.Priority == "High"), ct);

        var anomalyCount = await _db.AnomalyDetections
            .CountAsync(a => a.OrganizationId == orgId
                && a.DetectedAt >= DateTime.UtcNow.AddDays(-30), ct);

        var estimatedSavings = totalPredictions * 2500m;

        return new AIDashboardStats(totalPredictions, highPriority, anomalyCount, estimatedSavings);
    }
}
