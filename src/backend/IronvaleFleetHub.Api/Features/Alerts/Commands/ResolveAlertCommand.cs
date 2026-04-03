using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Alerts.Commands;

public record ResolveAlertCommand(Guid Id) : IRequest<Result<Alert>>, ISkipValidation;

public class ResolveAlertCommandHandler : IRequestHandler<ResolveAlertCommand, Result<Alert>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<ResolveAlertCommandHandler> _logger;

    public ResolveAlertCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        ILogger<ResolveAlertCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<Result<Alert>> Handle(ResolveAlertCommand request, CancellationToken ct)
    {
        var alert = await _db.Alerts
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.OrganizationId == _tenant.OrganizationId, ct);

        if (alert is null)
            return Result<Alert>.Failure("Not found.");

        if (alert.Status == "Resolved")
            return Result<Alert>.Failure("Alert is already resolved.");

        alert.Status = "Resolved";
        alert.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Alert {AlertId} resolved by user {UserId}", request.Id, _tenant.UserId);

        return Result<Alert>.Success(alert);
    }
}
