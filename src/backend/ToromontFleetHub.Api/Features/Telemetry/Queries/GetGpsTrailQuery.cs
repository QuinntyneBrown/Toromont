using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Common;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Telemetry.Queries;

public record GpsTrailPoint(DateTime Timestamp, double? Latitude, double? Longitude);

public record GetGpsTrailQuery(Guid EquipmentId, string Range) : IRequest<Result<List<GpsTrailPoint>>>;

public class GetGpsTrailQueryHandler : IRequestHandler<GetGpsTrailQuery, Result<List<GpsTrailPoint>>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetGpsTrailQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<List<GpsTrailPoint>>> Handle(GetGpsTrailQuery request, CancellationToken ct)
    {
        var equipmentExists = await _db.Equipment
            .AnyAsync(e => e.Id == request.EquipmentId && e.OrganizationId == _tenant.OrganizationId, ct);

        if (!equipmentExists)
            return Result<List<GpsTrailPoint>>.Failure("Not found.");

        var since = request.Range.ToLowerInvariant() switch
        {
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            "90d" => DateTime.UtcNow.AddDays(-90),
            _ => DateTime.UtcNow.AddHours(-24)
        };

        var trail = await _db.TelemetryEvents
            .Where(t => t.EquipmentId == request.EquipmentId && t.Timestamp >= since)
            .OrderBy(t => t.Timestamp)
            .AsNoTracking()
            .Select(t => new GpsTrailPoint(t.Timestamp, t.Latitude, t.Longitude))
            .ToListAsync(ct);

        return Result<List<GpsTrailPoint>>.Success(trail);
    }
}
