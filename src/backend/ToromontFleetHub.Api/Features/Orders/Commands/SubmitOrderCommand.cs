using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Common;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Orders.Commands;

public record SubmitOrderCommand : IRequest<Result<PartsOrder>>;

public class SubmitOrderCommandHandler : IRequestHandler<SubmitOrderCommand, Result<PartsOrder>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<SubmitOrderCommandHandler> _logger;

    public SubmitOrderCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        ILogger<SubmitOrderCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<Result<PartsOrder>> Handle(SubmitOrderCommand request, CancellationToken ct)
    {
        var cartItems = await _db.CartItems
            .Include(c => c.Part)
            .Where(c => c.UserId == _tenant.UserId)
            .ToListAsync(ct);

        if (cartItems.Count == 0)
            return Result<PartsOrder>.Failure("Cart is empty.");

        var today = DateTime.UtcNow;
        var dateStr = today.ToString("yyyyMMdd");
        var dailyCount = await _db.PartsOrders
            .CountAsync(o => o.OrderNumber.StartsWith($"ORD-{dateStr}"), ct);

        var orderNumber = $"ORD-{dateStr}-{(dailyCount + 1):D3}";

        var order = new PartsOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = _tenant.OrganizationId,
            OrderNumber = orderNumber,
            UserId = _tenant.UserId,
            Status = "Submitted",
            CreatedAt = today
        };

        decimal subtotal = 0;
        foreach (var cartItem in cartItems)
        {
            var part = cartItem.Part!;
            var lineTotal = part.Price * cartItem.Quantity;
            subtotal += lineTotal;

            order.LineItems.Add(new OrderLineItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PartId = cartItem.PartId,
                PartNumber = part.PartNumber,
                Name = part.Name,
                UnitPrice = part.Price,
                Quantity = cartItem.Quantity,
                LineTotal = lineTotal
            });
        }

        order.Subtotal = subtotal;

        _db.PartsOrders.Add(order);
        _db.CartItems.RemoveRange(cartItems);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderNumber} submitted by user {UserId}, total {Subtotal}",
            orderNumber, _tenant.UserId, subtotal);

        return Result<PartsOrder>.Success(order);
    }
}
