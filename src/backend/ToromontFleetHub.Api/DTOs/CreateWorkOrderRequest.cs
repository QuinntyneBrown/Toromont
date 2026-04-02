namespace ToromontFleetHub.Api.DTOs;

public class CreateWorkOrderRequest
{
    public Guid EquipmentId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Description { get; set; } = string.Empty;
    public DateTime RequestedDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
}
