namespace IronvaleFleetHub.Api.Services;

public sealed class DevNotificationDeliveryOptions
{
    public bool Enabled { get; set; }
    public bool UseSmtp { get; set; } = true;
    public string? SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 1025;
    public string EmailDropFolder { get; set; } = "artifacts/dev-emails";
    public bool EnableSmsConsoleSink { get; set; } = true;
}
