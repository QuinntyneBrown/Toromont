namespace ToromontFleetHub.Api.Models;

public class EquipmentModelThreshold
{
    public Guid Id { get; set; }
    public string EquipmentModel { get; set; } = string.Empty;
    public double MaxTemperature { get; set; }
    public double MaxFuelConsumptionRate { get; set; }
    public double MaxOperatingHoursPerDay { get; set; }
}
