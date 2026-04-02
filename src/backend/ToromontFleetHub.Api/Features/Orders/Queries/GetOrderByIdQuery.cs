using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Orders.Queries;

public record GetOrderByIdQuery(Guid Id) : IRequest<PartsOrder?>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, PartsOrder?>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetOrderByIdQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PartsOrder?> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        return await _db.PartsOrders
            .Include(o => o.LineItems)
            .Include(o => o.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.Id && o.OrganizationId == _tenant.OrganizationId, ct);
    }
}
