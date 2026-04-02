using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Telemetry.Queries;

public record GetLatestTelemetryQuery(Guid EquipmentId) : IRequest<Result<TelemetryEvent?>>;

public class GetLatestTelemetryQueryHandler : IRequestHandler<GetLatestTelemetryQuery, Result<TelemetryEvent?>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetLatestTelemetryQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<TelemetryEvent?>> Handle(GetLatestTelemetryQuery request, CancellationToken ct)
    {
        var equipmentExists = await _db.Equipment
            .AnyAsync(e => e.Id == request.EquipmentId && e.OrganizationId == _tenant.OrganizationId, ct);

        if (!equipmentExists)
            return Result<TelemetryEvent?>.Failure("Not found.");

        var latest = await _db.TelemetryEvents
            .Where(t => t.EquipmentId == request.EquipmentId)
            .OrderByDescending(t => t.Timestamp)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        return Result<TelemetryEvent?>.Success(latest);
    }
}
