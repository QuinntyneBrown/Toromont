using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Cart.Commands;

public record RemoveCartItemCommand(Guid Id) : IRequest<Result<bool>>, ISkipValidation;

public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, Result<bool>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public RemoveCartItemCommandHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<bool>> Handle(RemoveCartItemCommand request, CancellationToken ct)
    {
        var item = await _db.CartItems
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == _tenant.UserId, ct);

        if (item is null)
            return Result<bool>.Failure("Not found.");

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}
