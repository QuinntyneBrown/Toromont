namespace IronvaleFleetHub.Api.DTOs;

public class AddCartItemRequest
{
    public Guid PartId { get; set; }
    public int Quantity { get; set; } = 1;
}
