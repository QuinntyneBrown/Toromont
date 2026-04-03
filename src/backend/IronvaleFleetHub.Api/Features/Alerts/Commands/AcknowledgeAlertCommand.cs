using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Alerts.Commands;

public record AcknowledgeAlertCommand(Guid Id) : IRequest<Result<Alert>>, ISkipValidation;

public class AcknowledgeAlertCommandHandler : IRequestHandler<AcknowledgeAlertCommand, Result<Alert>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<AcknowledgeAlertCommandHandler> _logger;

    public AcknowledgeAlertCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        ILogger<AcknowledgeAlertCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<Result<Alert>> Handle(AcknowledgeAlertCommand request, CancellationToken ct)
    {
        var alert = await _db.Alerts
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.OrganizationId == _tenant.OrganizationId, ct);

        if (alert is null)
            return Result<Alert>.Failure("Not found.");

        if (alert.Status != "Active")
            return Result<Alert>.Failure("Alert is not in active state.");

        alert.Status = "Acknowledged";
        alert.AcknowledgedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Alert {AlertId} acknowledged by user {UserId}", request.Id, _tenant.UserId);

        return Result<Alert>.Success(alert);
    }
}
