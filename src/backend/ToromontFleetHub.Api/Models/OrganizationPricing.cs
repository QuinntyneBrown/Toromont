namespace ToromontFleetHub.Api.Models;

public class OrganizationPricing
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PartId { get; set; }
    public decimal Price { get; set; }
}
