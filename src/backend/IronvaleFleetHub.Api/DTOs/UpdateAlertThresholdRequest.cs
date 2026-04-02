namespace IronvaleFleetHub.Api.DTOs;

public class UpdateAlertThresholdRequest
{
    public string MetricName { get; set; } = string.Empty;
    public double WarningValue { get; set; }
    public double CriticalValue { get; set; }
    public Guid? EquipmentId { get; set; }
}
