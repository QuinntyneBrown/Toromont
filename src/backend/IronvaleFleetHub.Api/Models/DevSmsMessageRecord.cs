namespace IronvaleFleetHub.Api.Models;

/// <summary>
/// Development diagnostic entity — stores captured SMS messages for inspection via DevNotificationController.
/// Not tenant-scoped; used for developer inspection only.
/// </summary>
public class DevSmsMessageRecord
{
    public Guid Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
