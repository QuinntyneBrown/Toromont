namespace IronvaleFleetHub.Api.Models;

public class DevPartsOrderEvent
{
    public Guid Id { get; set; }
    public Guid PartsOrderId { get; set; }
    public string ExternalEventId { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public DateTime EventTimestamp { get; set; }
    public bool Processed { get; set; }
}
