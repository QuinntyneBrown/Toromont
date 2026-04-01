namespace ToromontFleetHub.Api.Models;

public class WorkOrder
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public Guid EquipmentId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Open, InProgress, Completed, Cancelled
    public string Description { get; set; } = string.Empty;
    public DateTime RequestedDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Equipment? Equipment { get; set; }
    public User? AssignedTo { get; set; }
    public List<WorkOrderHistory> History { get; set; } = new();
}
