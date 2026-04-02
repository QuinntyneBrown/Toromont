using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Telemetry.Commands;

public record IngestTelemetryCommand(
    Guid EquipmentId,
    string EventType,
    double? EngineHours,
    double? FuelLevel,
    double? Temperature,
    double? Latitude,
    double? Longitude,
    string? Payload
) : IRequest<Result<Guid>>;

public class IngestTelemetryCommandHandler : IRequestHandler<IngestTelemetryCommand, Result<Guid>>
{
    private readonly FleetHubDbContext _db;
    private readonly IAlertEvaluatorService _alertEvaluator;

    public IngestTelemetryCommandHandler(FleetHubDbContext db, IAlertEvaluatorService alertEvaluator)
    {
        _db = db;
        _alertEvaluator = alertEvaluator;
    }

    public async Task<Result<Guid>> Handle(IngestTelemetryCommand request, CancellationToken ct)
    {
        var equipment = await _db.Equipment
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EquipmentId, ct);

        if (equipment is null)
            return Result<Guid>.Failure("Equipment not found.");

        var telemetryEvent = new TelemetryEvent
        {
            Id = Guid.NewGuid(),
            EquipmentId = request.EquipmentId,
            EventType = request.EventType,
            Timestamp = DateTime.UtcNow,
            EngineHours = request.EngineHours ?? 0,
            FuelLevel = request.FuelLevel ?? 0,
            Temperature = request.Temperature ?? 0,
            Latitude = request.Latitude ?? 0,
            Longitude = request.Longitude ?? 0,
            Payload = request.Payload
        };

        _db.TelemetryEvents.Add(telemetryEvent);
        await _db.SaveChangesAsync(ct);

        await _alertEvaluator.EvaluateAsync(telemetryEvent);

        return Result<Guid>.Success(telemetryEvent.Id);
    }
}
