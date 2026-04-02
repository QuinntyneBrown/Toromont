namespace IronvaleFleetHub.Api.Models;

public class PartsOrder
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Status { get; set; } = "Submitted";
    public decimal Subtotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public User? User { get; set; }
    public List<OrderLineItem> LineItems { get; set; } = new();
}
