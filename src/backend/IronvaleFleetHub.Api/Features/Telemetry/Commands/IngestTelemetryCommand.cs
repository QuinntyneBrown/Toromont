using FluentValidation;
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
    private readonly ILogger<IngestTelemetryCommandHandler> _logger;

    public IngestTelemetryCommandHandler(FleetHubDbContext db, ILogger<IngestTelemetryCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
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
            OrganizationId = equipment.OrganizationId,
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

        _logger.LogInformation(
            "Telemetry event {EventId} ingested via API for equipment {EquipmentId}. Alert evaluation deferred to Functions pipeline.",
            telemetryEvent.Id, request.EquipmentId);

        return Result<Guid>.Success(telemetryEvent.Id);
    }
}

public class IngestTelemetryCommandValidator : AbstractValidator<IngestTelemetryCommand>
{
    public IngestTelemetryCommandValidator()
    {
        RuleFor(x => x.EquipmentId).NotEmpty();
        RuleFor(x => x.EventType).NotEmpty();
    }
}
