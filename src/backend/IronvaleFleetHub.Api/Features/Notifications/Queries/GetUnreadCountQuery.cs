using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Notifications.Queries;

public record GetUnreadCountQuery : IRequest<int>;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetUnreadCountQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken ct)
    {
        return await _db.Notifications
            .CountAsync(n => n.UserId == _tenant.UserId && !n.IsRead, ct);
    }
}
