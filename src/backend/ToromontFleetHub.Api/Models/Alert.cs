namespace ToromontFleetHub.Api.Models;

public class Alert
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EquipmentId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Equipment? Equipment { get; set; }
}
