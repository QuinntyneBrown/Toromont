using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Common;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Telemetry.Commands;

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
            EngineHours = request.EngineHours,
            FuelLevel = request.FuelLevel,
            Temperature = request.Temperature,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Payload = request.Payload
        };

        _db.TelemetryEvents.Add(telemetryEvent);
        await _db.SaveChangesAsync(ct);

        await _alertEvaluator.EvaluateAsync(telemetryEvent);

        return Result<Guid>.Success(telemetryEvent.Id);
    }
}
