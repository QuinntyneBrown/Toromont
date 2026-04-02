namespace IronvaleFleetHub.Api.Models;

public class AlertThreshold
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? EquipmentId { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public double WarningValue { get; set; }
    public double CriticalValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
