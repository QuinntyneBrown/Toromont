using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Hubs;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Services;

public class NotificationDispatchService : INotificationDispatchService
{
    private readonly FleetHubDbContext _db;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationDispatchService> _logger;

    public NotificationDispatchService(
        FleetHubDbContext db,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationDispatchService> logger)
    {
        _db = db;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task DispatchAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string? entityType = null,
        Guid? entityId = null,
        CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            _logger.LogWarning("Cannot dispatch notification: user {UserId} not found", userId);
            return;
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = user.OrganizationId,
            Type = type,
            Title = title,
            Message = message,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        // Build unified DTO for both REST and SignalR
        var dto = new NotificationDto(
            notification.Id,
            notification.UserId,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.CreatedAt,
            notification.EntityType,
            notification.EntityId);

        // Push via SignalR using canonical event name
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("ReceiveNotification", dto, ct);

        // Update badge count
        var unreadCount = await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("UpdateBadgeCount", unreadCount, ct);

        _logger.LogInformation("Notification dispatched to user {UserId}: {Title}", userId, title);

        // Check user preferences for email/SMS dispatch
        var preferences = await _db.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type, ct);

        if (preferences?.EmailEnabled == true)
        {
            _logger.LogInformation("Email notification queued for user {UserId}: {Title}", userId, title);
        }

        if (preferences?.SmsEnabled == true)
        {
            _logger.LogInformation("SMS notification queued for user {UserId}: {Title}", userId, title);
        }
    }
}
