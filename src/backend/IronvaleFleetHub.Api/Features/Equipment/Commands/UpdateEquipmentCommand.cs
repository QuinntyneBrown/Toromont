using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Equipment.Commands;

public record UpdateEquipmentCommand(
    Guid Id,
    string? Name,
    string? Status,
    string? Location,
    double? Latitude,
    double? Longitude,
    string? GpsDeviceId,
    string? Notes
) : IRequest<Result<Models.Equipment>>;

public class UpdateEquipmentCommandHandler : IRequestHandler<UpdateEquipmentCommand, Result<Models.Equipment>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdateEquipmentCommandHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<Models.Equipment>> Handle(UpdateEquipmentCommand request, CancellationToken ct)
    {
        var equipment = await _db.Equipment
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.OrganizationId == _tenant.OrganizationId, ct);

        if (equipment is null)
            return Result<Models.Equipment>.Failure("Not found.");

        if (request.Name is not null) equipment.Name = request.Name;
        if (request.Status is not null) equipment.Status = request.Status;
        if (request.Location is not null) equipment.Location = request.Location;
        if (request.Latitude.HasValue) equipment.Latitude = request.Latitude.Value;
        if (request.Longitude.HasValue) equipment.Longitude = request.Longitude.Value;
        if (request.GpsDeviceId is not null) equipment.GpsDeviceId = request.GpsDeviceId;
        if (request.Notes is not null) equipment.Notes = request.Notes;
        equipment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result<Models.Equipment>.Success(equipment);
    }
}
