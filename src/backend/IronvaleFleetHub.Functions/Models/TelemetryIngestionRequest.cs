using System.Text.Json.Serialization;

namespace IronvaleFleetHub.Functions.Models;

public class TelemetryIngestionRequest
{
    [JsonPropertyName("equipmentId")]
    public Guid EquipmentId { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public TelemetryPayload? Payload { get; set; }
}

public class TelemetryPayload
{
    [JsonPropertyName("engineHours")]
    public double? EngineHours { get; set; }

    [JsonPropertyName("fuelLevel")]
    public double? FuelLevel { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }
}
