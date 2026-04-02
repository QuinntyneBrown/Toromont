using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Common;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Equipment.Commands;

public record CreateEquipmentCommand(
    string Name,
    string Make,
    string Model,
    int Year,
    string SerialNumber,
    string Category,
    string? Location,
    double? Latitude,
    double? Longitude,
    string? GpsDeviceId,
    DateTime? PurchaseDate,
    DateTime? WarrantyExpiration,
    string? Notes
) : IRequest<Result<Models.Equipment>>;

public class CreateEquipmentCommandValidator : AbstractValidator<CreateEquipmentCommand>
{
    public CreateEquipmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Make).NotEmpty();
        RuleFor(x => x.Model).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 2);
        RuleFor(x => x.SerialNumber).NotEmpty();
        RuleFor(x => x.Category).NotEmpty();
    }
}

public class CreateEquipmentCommandHandler : IRequestHandler<CreateEquipmentCommand, Result<Models.Equipment>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<CreateEquipmentCommandHandler> _logger;

    public CreateEquipmentCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        ILogger<CreateEquipmentCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<Result<Models.Equipment>> Handle(CreateEquipmentCommand request, CancellationToken ct)
    {
        var exists = await _db.Equipment.AnyAsync(
            e => e.SerialNumber == request.SerialNumber
              && e.OrganizationId == _tenant.OrganizationId, ct);

        if (exists)
            return Result<Models.Equipment>.Failure(
                "Equipment with this serial number already exists in this organization.");

        var equipment = new Models.Equipment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _tenant.OrganizationId,
            Name = request.Name,
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            SerialNumber = request.SerialNumber,
            Category = request.Category,
            Location = request.Location,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            GpsDeviceId = request.GpsDeviceId,
            PurchaseDate = request.PurchaseDate,
            WarrantyExpiration = request.WarrantyExpiration,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Equipment.Add(equipment);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Equipment {EquipmentId} created by user {UserId}",
            equipment.Id, _tenant.UserId);

        return Result<Models.Equipment>.Success(equipment);
    }
}
