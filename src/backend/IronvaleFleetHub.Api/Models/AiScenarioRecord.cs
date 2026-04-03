namespace IronvaleFleetHub.Api.Models;

public class AiScenarioRecord
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string ScenarioType { get; set; } = string.Empty;
    public string PredictedIssue { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Explanation { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
