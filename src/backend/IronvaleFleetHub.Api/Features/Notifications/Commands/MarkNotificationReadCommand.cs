using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Notifications.Commands;

public record MarkNotificationReadCommand(Guid Id) : IRequest<Result<Notification>>, ISkipValidation;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result<Notification>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public MarkNotificationReadCommandHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<Notification>> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == _tenant.UserId, ct);

        if (notification is null)
            return Result<Notification>.Failure("Not found.");

        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);

        return Result<Notification>.Success(notification);
    }
}
