using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Notifications.Commands;

public record MarkAllNotificationsReadCommand : IRequest, ISkipValidation;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public MarkAllNotificationsReadCommandHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == _tenant.UserId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread)
            n.IsRead = true;

        await _db.SaveChangesAsync(ct);
    }
}
