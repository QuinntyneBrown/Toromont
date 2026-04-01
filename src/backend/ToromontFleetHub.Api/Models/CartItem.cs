namespace ToromontFleetHub.Api.Models;

public class CartItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PartId { get; set; }
    public int Quantity { get; set; }

    public Part? Part { get; set; }
}
