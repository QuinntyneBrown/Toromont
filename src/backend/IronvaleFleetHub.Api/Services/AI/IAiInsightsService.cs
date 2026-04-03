namespace IronvaleFleetHub.Api.Services.AI;

public interface IAiInsightsService
{
    Task<MaintenancePrediction> PredictMaintenanceAsync(Guid equipmentId, CancellationToken ct = default);
    Task<IReadOnlyList<AnomalyInsight>> DetectAnomaliesAsync(Guid equipmentId, CancellationToken ct = default);
}

public class MaintenancePrediction
{
    public Guid EquipmentId { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string PredictedIssue { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Explanation { get; set; } = string.Empty;
}

public class AnomalyInsight
{
    public string Metric { get; set; } = string.Empty;
    public decimal Baseline { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal DeviationPercent { get; set; }
    public string Severity { get; set; } = "Medium";
    public string Explanation { get; set; } = string.Empty;
}
