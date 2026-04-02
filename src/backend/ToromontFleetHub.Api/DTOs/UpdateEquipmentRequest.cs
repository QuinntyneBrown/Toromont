namespace ToromontFleetHub.Api.DTOs;

public class UpdateEquipmentRequest
{
    public string? Name { get; set; }
    public string? Status { get; set; }
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? GpsDeviceId { get; set; }
    public string? Notes { get; set; }
}
