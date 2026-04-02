using MediatR;
using Microsoft.AspNetCore.SignalR;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Hubs;
using ToromontFleetHub.Api.Models;

namespace ToromontFleetHub.Api.Features.WorkOrders.Notifications;

public record WorkOrderCreatedNotification(
    Guid WorkOrderId,
    string WorkOrderNumber,
    Guid? AssignedToUserId,
    Guid OrganizationId
) : INotification;

public class WorkOrderCreatedNotificationHandler : INotificationHandler<WorkOrderCreatedNotification>
{
    private readonly FleetHubDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<WorkOrderCreatedNotificationHandler> _logger;

    public WorkOrderCreatedNotificationHandler(
        FleetHubDbContext db,
        IHubContext<NotificationHub> hub,
        ILogger<WorkOrderCreatedNotificationHandler> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    public async Task Handle(WorkOrderCreatedNotification notification, CancellationToken ct)
    {
        if (notification.AssignedToUserId is null) return;

        var record = new Notification
        {
            Id = Guid.NewGuid(),
            OrganizationId = notification.OrganizationId,
            UserId = notification.AssignedToUserId.Value,
            Type = "WorkOrderAssigned",
            Title = "New Work Order Assigned",
            Message = $"Work order {notification.WorkOrderNumber} has been assigned to you.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(record);
        await _db.SaveChangesAsync(ct);

        try
        {
            await _hub.Clients
                .User(notification.AssignedToUserId.Value.ToString())
                .SendAsync("NotificationReceived", record, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR push failed for user {UserId}", notification.AssignedToUserId);
        }
    }
}
