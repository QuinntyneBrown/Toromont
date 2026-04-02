using System.Text.Json.Serialization;

namespace ToromontFleetHub.Api.Models;

public class OrderLineItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid PartId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    [JsonIgnore]
    public PartsOrder? Order { get; set; }
}
