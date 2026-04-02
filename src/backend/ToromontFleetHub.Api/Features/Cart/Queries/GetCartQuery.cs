using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Cart.Queries;

public record GetCartQuery : IRequest<List<CartItem>>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, List<CartItem>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetCartQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<CartItem>> Handle(GetCartQuery request, CancellationToken ct)
    {
        return await _db.CartItems
            .Include(c => c.Part)
            .Where(c => c.UserId == _tenant.UserId)
            .AsNoTracking()
            .OrderByDescending(c => c.AddedAt)
            .ToListAsync(ct);
    }
}
