namespace IronvaleFleetHub.Api.Models;

public class AnomalyDetection
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EquipmentId { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double ExpectedValue { get; set; }
    public double ActualValue { get; set; }
    public double DeviationSigma { get; set; }
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public Equipment? Equipment { get; set; }
}
