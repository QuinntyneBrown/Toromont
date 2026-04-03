using IronvaleFleetHub.Api.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Orders.Queries;

public record GetOrderListQuery(int Skip, int Take) : IRequest<PaginatedResponse<PartsOrder>>;

public class GetOrderListQueryHandler : IRequestHandler<GetOrderListQuery, PaginatedResponse<PartsOrder>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetOrderListQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResponse<PartsOrder>> Handle(GetOrderListQuery request, CancellationToken ct)
    {
        var query = _db.PartsOrders
            .Include(o => o.LineItems)
            .Where(o => o.UserId == _tenant.UserId && o.OrganizationId == _tenant.OrganizationId)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking();

        var total = await query.CountAsync(ct);
        var items = await query.Skip(request.Skip).Take(request.Take).ToListAsync(ct);

        return new PaginatedResponse<PartsOrder>
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
