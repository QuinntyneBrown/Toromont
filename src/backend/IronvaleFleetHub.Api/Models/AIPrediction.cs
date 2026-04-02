namespace IronvaleFleetHub.Api.Models;

public class AIPrediction
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EquipmentId { get; set; }
    public string Component { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public bool IsDismissed { get; set; }
    public DateTime GeneratedAt { get; set; }
    public Equipment? Equipment { get; set; }
}
