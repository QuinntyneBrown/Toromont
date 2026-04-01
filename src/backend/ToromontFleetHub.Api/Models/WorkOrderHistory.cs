namespace ToromontFleetHub.Api.Models;

public class WorkOrderHistory
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public User? ChangedBy { get; set; }
}
