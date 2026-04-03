using IronvaleFleetHub.Api.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Cart.Queries;

public record GetCartQuery : IRequest<List<CartItem>>, ISkipValidation;

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