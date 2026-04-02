using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Notifications.Queries;

public record GetNotificationPreferencesQuery : IRequest<List<NotificationPreference>>;

public class GetNotificationPreferencesQueryHandler : IRequestHandler<GetNotificationPreferencesQuery, List<NotificationPreference>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetNotificationPreferencesQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<NotificationPreference>> Handle(GetNotificationPreferencesQuery request, CancellationToken ct)
    {
        return await _db.NotificationPreferences
            .Where(p => p.UserId == _tenant.UserId)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
