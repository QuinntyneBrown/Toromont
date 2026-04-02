using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Alerts.Commands;

public record UpdateAlertThresholdCommand(
    Guid Id,
    string MetricName,
    double WarningValue,
    double CriticalValue,
    Guid? EquipmentId = null
) : IRequest<Result<AlertThreshold>>;

public class UpdateAlertThresholdCommandHandler : IRequestHandler<UpdateAlertThresholdCommand, Result<AlertThreshold>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdateAlertThresholdCommandHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<AlertThreshold>> Handle(UpdateAlertThresholdCommand request, CancellationToken ct)
    {
        var threshold = await _db.AlertThresholds
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.OrganizationId == _tenant.OrganizationId, ct);

        if (threshold is null)
            return Result<AlertThreshold>.Failure("Not found.");

        threshold.MetricName = request.MetricName;
        threshold.WarningValue = request.WarningValue;
        threshold.CriticalValue = request.CriticalValue;
        threshold.EquipmentId = request.EquipmentId;
        threshold.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Result<AlertThreshold>.Success(threshold);
    }
}
