using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Common;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Cart.Commands;

public record AddCartItemCommand(Guid PartId, int Quantity) : IRequest<Result<CartItem>>;

public class AddCartItemCommandHandler : IRequestHandler<AddCartItemCommand, Result<CartItem>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public AddCartItemCommandHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<CartItem>> Handle(AddCartItemCommand request, CancellationToken ct)
    {
        var part = await _db.Parts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PartId, ct);
        if (part is null)
            return Result<CartItem>.Failure("Part not found.");

        var existing = await _db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == _tenant.UserId && c.PartId == request.PartId, ct);

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
            await _db.SaveChangesAsync(ct);
            existing.Part = part;
            return Result<CartItem>.Success(existing);
        }

        var item = new CartItem
        {
            Id = Guid.NewGuid(),
            UserId = _tenant.UserId,
            PartId = request.PartId,
            Quantity = request.Quantity > 0 ? request.Quantity : 1,
            AddedAt = DateTime.UtcNow
        };

        _db.CartItems.Add(item);
        await _db.SaveChangesAsync(ct);

        item.Part = part;
        return Result<CartItem>.Success(item);
    }
}
