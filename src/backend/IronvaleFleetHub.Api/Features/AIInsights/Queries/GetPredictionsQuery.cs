using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.AIInsights.Queries;

public record GetPredictionsQuery(string? Sort) : IRequest<List<AIPrediction>>;

public class GetPredictionsQueryHandler : IRequestHandler<GetPredictionsQuery, List<AIPrediction>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPredictionsQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<AIPrediction>> Handle(GetPredictionsQuery request, CancellationToken ct)
    {
        var query = _db.AIPredictions
            .Include(p => p.Equipment)
            .Where(p => p.OrganizationId == _tenant.OrganizationId && !p.IsDismissed)
            .AsNoTracking();

        query = request.Sort?.ToLowerInvariant() switch
        {
            "confidence" => query.OrderByDescending(p => p.ConfidenceScore),
            "-confidence" => query.OrderBy(p => p.ConfidenceScore),
            "priority" => query.OrderByDescending(p => p.Priority == "Critical" ? 0 :
                                                        p.Priority == "High" ? 1 :
                                                        p.Priority == "Medium" ? 2 : 3),
            _ => query.OrderByDescending(p => p.ConfidenceScore)
        };

        return await query.ToListAsync(ct);
    }
}
