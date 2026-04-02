namespace IronvaleFleetHub.Api.DTOs;

public class CreateEquipmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? GpsDeviceId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiration { get; set; }
    public string? Notes { get; set; }
}
