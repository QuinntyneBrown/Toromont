using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Cart.Commands;

public record UpdateCartItemCommand(Guid Id, int Quantity) : IRequest<Result<CartItem>>;

public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, Result<CartItem>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdateCartItemCommandHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<CartItem>> Handle(UpdateCartItemCommand request, CancellationToken ct)
    {
        var item = await _db.CartItems
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == _tenant.UserId, ct);

        if (item is null)
            return Result<CartItem>.Failure("Not found.");

        if (request.Quantity <= 0)
            return Result<CartItem>.Failure("Quantity must be greater than zero.");

        item.Quantity = request.Quantity;
        await _db.SaveChangesAsync(ct);

        return Result<CartItem>.Success(item);
    }
}
