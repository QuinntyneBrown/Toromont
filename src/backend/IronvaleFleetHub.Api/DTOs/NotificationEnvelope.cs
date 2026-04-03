namespace IronvaleFleetHub.Api.DTOs;

public sealed record NotificationEnvelope(
    Guid UserId,
    string EventType,
    string Title,
    string Message,
    Dictionary<string, string> Tokens,
    NotificationPriority Priority = NotificationPriority.Normal);

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}
