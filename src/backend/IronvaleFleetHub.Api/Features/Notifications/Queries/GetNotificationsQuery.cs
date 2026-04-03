using IronvaleFleetHub.Api.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Notifications.Queries;

public record GetNotificationsQuery(int Skip, int Take) : IRequest<PaginatedResponse<Notification>>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, PaginatedResponse<Notification>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetNotificationsQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResponse<Notification>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var query = _db.Notifications
            .Where(n => n.UserId == _tenant.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .AsNoTracking();

        var total = await query.CountAsync(ct);
        var items = await query.Skip(request.Skip).Take(request.Take).ToListAsync(ct);

        return new PaginatedResponse<Notification>
        {
            Items = items,
            Pagination = new PaginationInfo
            {
                Page = request.Skip / request.Take + 1,
                PageSize = request.Take,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)request.Take)
            }
        };
    }
}
