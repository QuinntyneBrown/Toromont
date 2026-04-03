namespace IronvaleFleetHub.Api.Services.AI;

public class NarrativeFormatter
{
    public string FormatPrediction(PredictionEvidence evidence)
    {
        if (evidence.IsColdStart)
        {
            return $"Analysis for equipment {evidence.EquipmentId}: cold-start mode. " +
                   "Insufficient telemetry data available for reliable prediction. " +
                   $"Confidence: {evidence.ConfidenceScore:P0}. " +
                   "Recommendation: Continue collecting telemetry data for at least 30 days before relying on predictions.";
        }

        var factorSummary = evidence.Factors.Length > 0
            ? $"Key factors: {string.Join("; ", evidence.Factors)}."
            : "No significant anomalies detected.";

        return $"Predictive maintenance analysis completed with {evidence.ConfidenceScore:P0} confidence. " +
               $"Predicted issue: {evidence.PredictedIssue}. " +
               $"Priority: {evidence.Priority}. " +
               factorSummary + " " +
               $"Recommended action: {evidence.RecommendedAction}.";
    }

    public string FormatAnomaly(string metric, decimal baseline, decimal current, decimal deviation, string severity)
    {
        var direction = current > baseline ? "above" : "below";
        return $"{metric} anomaly detected: current value {current:F1} is {direction} baseline {baseline:F1} " +
               $"(deviation: {deviation:F1}%). " +
               $"Severity: {severity}. " +
               severity switch
               {
                   "High" => "Immediate investigation recommended.",
                   "Medium" => "Monitor closely and schedule inspection if trend continues.",
                   _ => "Continue routine monitoring."
               };
    }
}
