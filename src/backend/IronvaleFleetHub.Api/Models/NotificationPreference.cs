namespace IronvaleFleetHub.Api.Models;

public class NotificationPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
}
