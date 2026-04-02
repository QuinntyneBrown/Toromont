using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Equipment.Commands;

public record DeleteEquipmentCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteEquipmentCommandHandler : IRequestHandler<DeleteEquipmentCommand, Result<bool>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<DeleteEquipmentCommandHandler> _logger;

    public DeleteEquipmentCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        ILogger<DeleteEquipmentCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteEquipmentCommand request, CancellationToken ct)
    {
        var equipment = await _db.Equipment
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.OrganizationId == _tenant.OrganizationId, ct);

        if (equipment is null)
            return Result<bool>.Failure("Not found.");

        var telemetry = _db.TelemetryEvents.Where(t => t.EquipmentId == request.Id);
        var alerts = _db.Alerts.Where(a => a.EquipmentId == request.Id);
        var predictions = _db.AIPredictions.Where(p => p.EquipmentId == request.Id);
        var anomalies = _db.AnomalyDetections.Where(a => a.EquipmentId == request.Id);
        var workOrders = _db.WorkOrders.Where(w => w.EquipmentId == request.Id);

        _db.TelemetryEvents.RemoveRange(telemetry);
        _db.Alerts.RemoveRange(alerts);
        _db.AIPredictions.RemoveRange(predictions);
        _db.AnomalyDetections.RemoveRange(anomalies);

        foreach (var wo in await workOrders.Include(w => w.History).ToListAsync(ct))
        {
            _db.WorkOrderHistories.RemoveRange(wo.History);
            _db.WorkOrders.Remove(wo);
        }

        _db.Equipment.Remove(equipment);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Equipment {EquipmentId} deleted by user {UserId}", request.Id, _tenant.UserId);
        return Result<bool>.Success(true);
    }
}
