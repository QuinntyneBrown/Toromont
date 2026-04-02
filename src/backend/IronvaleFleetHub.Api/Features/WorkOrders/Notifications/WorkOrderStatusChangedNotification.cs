using MediatR;
using Microsoft.AspNetCore.SignalR;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Hubs;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Features.WorkOrders.Notifications;

public record WorkOrderStatusChangedNotification(
    Guid WorkOrderId,
    string WorkOrderNumber,
    string PreviousStatus,
    string NewStatus,
    Guid OrganizationId
) : INotification;

public class WorkOrderStatusChangedNotificationHandler : INotificationHandler<WorkOrderStatusChangedNotification>
{
    private readonly FleetHubDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<WorkOrderStatusChangedNotificationHandler> _logger;

    public WorkOrderStatusChangedNotificationHandler(
        FleetHubDbContext db,
        IHubContext<NotificationHub> hub,
        ILogger<WorkOrderStatusChangedNotificationHandler> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    public async Task Handle(WorkOrderStatusChangedNotification notification, CancellationToken ct)
    {
        var record = new Notification
        {
            Id = Guid.NewGuid(),
            OrganizationId = notification.OrganizationId,
            UserId = Guid.Empty,
            Type = "WorkOrderStatusChanged",
            Title = "Work Order Status Updated",
            Message = $"Work order {notification.WorkOrderNumber} status changed from '{notification.PreviousStatus}' to '{notification.NewStatus}'.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            EntityType = "WorkOrder",
            EntityId = notification.WorkOrderId
        };

        _db.Notifications.Add(record);
        await _db.SaveChangesAsync(ct);

        try
        {
            await _hub.Clients
                .Group($"org-{notification.OrganizationId}")
                .SendAsync("WorkOrderStatusChanged", new
                {
                    notification.WorkOrderId,
                    notification.WorkOrderNumber,
                    notification.PreviousStatus,
                    notification.NewStatus
                }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR push failed for work order status change {WorkOrderId}", notification.WorkOrderId);
        }
    }
}
