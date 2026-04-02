namespace ToromontFleetHub.Api.DTOs;

public class TelemetryIngestionRequest
{
    public Guid EquipmentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public double EngineHours { get; set; }
    public double FuelLevel { get; set; }
    public double Temperature { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Payload { get; set; }
}
