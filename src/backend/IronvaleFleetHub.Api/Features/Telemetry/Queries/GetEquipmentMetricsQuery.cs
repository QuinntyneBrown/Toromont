using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Telemetry.Queries;

public record TelemetryMetricPoint(DateTime Timestamp, double Value);

public record StructuredMetricsResponse(Guid EquipmentId, string Range, Dictionary<string, List<TelemetryMetricPoint>> Metrics);

public record GetEquipmentMetricsQuery(Guid EquipmentId, string Range, string? MetricNames = null) : IRequest<Result<StructuredMetricsResponse>>;

public class GetEquipmentMetricsQueryHandler : IRequestHandler<GetEquipmentMetricsQuery, Result<StructuredMetricsResponse>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetEquipmentMetricsQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<StructuredMetricsResponse>> Handle(GetEquipmentMetricsQuery request, CancellationToken ct)
    {
        var equipmentExists = await _db.Equipment
            .AnyAsync(e => e.Id == request.EquipmentId && e.OrganizationId == _tenant.OrganizationId, ct);

        if (!equipmentExists)
            return Result<StructuredMetricsResponse>.Failure("Not found.");

        var since = request.Range.ToLowerInvariant() switch
        {
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            "90d" => DateTime.UtcNow.AddDays(-90),
            _ => DateTime.UtcNow.AddHours(-24)
        };

        var requestedMetrics = string.IsNullOrWhiteSpace(request.MetricNames)
            ? new[] { "engineHours", "fuelLevel", "temperature" }
            : request.MetricNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var events = await _db.TelemetryEvents
            .Where(t => t.EquipmentId == request.EquipmentId && t.Timestamp >= since)
            .OrderBy(t => t.Timestamp)
            .AsNoTracking()
            .ToListAsync(ct);

        var metrics = new Dictionary<string, List<TelemetryMetricPoint>>();

        foreach (var metric in requestedMetrics)
        {
            metrics[metric] = metric.ToLowerInvariant() switch
            {
                "enginehours" => events.Select(e => new TelemetryMetricPoint(e.Timestamp, e.EngineHours)).ToList(),
                "fuellevel" => events.Select(e => new TelemetryMetricPoint(e.Timestamp, e.FuelLevel)).ToList(),
                "temperature" => events.Select(e => new TelemetryMetricPoint(e.Timestamp, e.Temperature)).ToList(),
                _ => new List<TelemetryMetricPoint>()
            };
        }

        return Result<StructuredMetricsResponse>.Success(
            new StructuredMetricsResponse(request.EquipmentId, request.Range, metrics));
    }
}
