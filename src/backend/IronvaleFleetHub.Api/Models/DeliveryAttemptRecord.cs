namespace IronvaleFleetHub.Api.Models;

/// <summary>
/// Development diagnostic entity — records each email/SMS delivery attempt and its outcome.
/// Not tenant-scoped; used for developer inspection only.
/// </summary>
public class DeliveryAttemptRecord
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
}
