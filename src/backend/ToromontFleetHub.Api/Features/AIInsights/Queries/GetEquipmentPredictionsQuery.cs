using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Common;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.AIInsights.Queries;

public record GetEquipmentPredictionsQuery(Guid EquipmentId) : IRequest<Result<List<AIPrediction>>>;

public class GetEquipmentPredictionsQueryHandler : IRequestHandler<GetEquipmentPredictionsQuery, Result<List<AIPrediction>>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetEquipmentPredictionsQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<List<AIPrediction>>> Handle(GetEquipmentPredictionsQuery request, CancellationToken ct)
    {
        var equipmentExists = await _db.Equipment
            .AnyAsync(e => e.Id == request.EquipmentId && e.OrganizationId == _tenant.OrganizationId, ct);

        if (!equipmentExists)
            return Result<List<AIPrediction>>.Failure("Not found.");

        var predictions = await _db.AIPredictions
            .Where(p => p.EquipmentId == request.EquipmentId && !p.IsDismissed)
            .OrderByDescending(p => p.ConfidenceScore)
            .AsNoTracking()
            .ToListAsync(ct);

        return Result<List<AIPrediction>>.Success(predictions);
    }
}
