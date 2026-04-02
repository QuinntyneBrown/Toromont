namespace IronvaleFleetHub.Api.Models;

public class TelemetryEvent
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double EngineHours { get; set; }
    public double FuelLevel { get; set; }
    public double Temperature { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Payload { get; set; }
}
