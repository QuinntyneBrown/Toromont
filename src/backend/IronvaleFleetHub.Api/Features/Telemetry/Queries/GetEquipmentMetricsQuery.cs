using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Telemetry.Queries;

public record TelemetryMetricPoint(DateTime Timestamp, double? EngineHours, double? FuelLevel, double? Temperature);

public record GetEquipmentMetricsQuery(Guid EquipmentId, string Range) : IRequest<Result<List<TelemetryMetricPoint>>>;

public class GetEquipmentMetricsQueryHandler : IRequestHandler<GetEquipmentMetricsQuery, Result<List<TelemetryMetricPoint>>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetEquipmentMetricsQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<List<TelemetryMetricPoint>>> Handle(GetEquipmentMetricsQuery request, CancellationToken ct)
    {
        var equipmentExists = await _db.Equipment
            .AnyAsync(e => e.Id == request.EquipmentId && e.OrganizationId == _tenant.OrganizationId, ct);

        if (!equipmentExists)
            return Result<List<TelemetryMetricPoint>>.Failure("Not found.");

        var since = request.Range.ToLowerInvariant() switch
        {
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            "90d" => DateTime.UtcNow.AddDays(-90),
            _ => DateTime.UtcNow.AddHours(-24)
        };

        var metrics = await _db.TelemetryEvents
            .Where(t => t.EquipmentId == request.EquipmentId && t.Timestamp >= since)
            .OrderBy(t => t.Timestamp)
            .AsNoTracking()
            .Select(t => new TelemetryMetricPoint(t.Timestamp, t.EngineHours, t.FuelLevel, t.Temperature))
            .ToListAsync(ct);

        return Result<List<TelemetryMetricPoint>>.Success(metrics);
    }
}
