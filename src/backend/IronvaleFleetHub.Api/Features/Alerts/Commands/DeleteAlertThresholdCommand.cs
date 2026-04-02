using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Alerts.Commands;

public record DeleteAlertThresholdCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteAlertThresholdCommandHandler : IRequestHandler<DeleteAlertThresholdCommand, Result<bool>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public DeleteAlertThresholdCommandHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<bool>> Handle(DeleteAlertThresholdCommand request, CancellationToken ct)
    {
        var threshold = await _db.AlertThresholds
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.OrganizationId == _tenant.OrganizationId, ct);

        if (threshold is null)
            return Result<bool>.Failure("Not found.");

        _db.AlertThresholds.Remove(threshold);
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}
