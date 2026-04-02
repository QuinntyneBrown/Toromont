namespace IronvaleFleetHub.Functions.Models;

public class TelemetryDeadLetterEntry
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string OriginalPayload { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
    public int RetryCount { get; set; }
    public bool IsReprocessed { get; set; }
}
