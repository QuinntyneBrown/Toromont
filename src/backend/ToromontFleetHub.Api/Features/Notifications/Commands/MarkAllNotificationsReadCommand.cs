using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Notifications.Commands;

public record MarkAllNotificationsReadCommand : IRequest;

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
