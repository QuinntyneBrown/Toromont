using MediatR;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;

namespace ToromontFleetHub.Api.Features.Orders.Notifications;

public record OrderSubmittedNotification(
    Guid OrderId,
    string OrderNumber,
    Guid UserId,
    Guid OrganizationId,
    decimal Subtotal
) : INotification;

public class OrderSubmittedNotificationHandler : INotificationHandler<OrderSubmittedNotification>
{
    private readonly FleetHubDbContext _db;
    private readonly ILogger<OrderSubmittedNotificationHandler> _logger;

    public OrderSubmittedNotificationHandler(FleetHubDbContext db, ILogger<OrderSubmittedNotificationHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(OrderSubmittedNotification notification, CancellationToken ct)
    {
        var record = new Notification
        {
            Id = Guid.NewGuid(),
            OrganizationId = notification.OrganizationId,
            UserId = notification.UserId,
            Type = "OrderSubmitted",
            Title = "Order Submitted",
            Message = $"Your order {notification.OrderNumber} has been submitted successfully (Total: ${notification.Subtotal:F2}).",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            EntityType = "Order",
            EntityId = notification.OrderId
        };

        _db.Notifications.Add(record);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist order submitted notification for order {OrderId}", notification.OrderId);
        }
    }
}
