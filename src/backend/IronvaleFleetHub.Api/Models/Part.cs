namespace IronvaleFleetHub.Api.Models;

public class Part
{
    public Guid Id { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Availability { get; set; } = "InStock";
    public string CompatibleModels { get; set; } = string.Empty;
}
