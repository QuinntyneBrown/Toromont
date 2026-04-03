namespace IronvaleFleetHub.Api.DTOs;

/// <summary>
/// Unified notification payload used for both REST API responses and SignalR delivery.
/// Both channels use the same shape to ensure consistency.
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    Guid UserId,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    string? EntityType,
    Guid? EntityId);
