using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Services.AI;

public class RuleBasedPredictionEngine
{
    private readonly FleetHubDbContext _db;
    private readonly ILogger<RuleBasedPredictionEngine> _logger;

    public RuleBasedPredictionEngine(FleetHubDbContext db, ILogger<RuleBasedPredictionEngine> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PredictionEvidence> EvaluateAsync(Guid equipmentId, CancellationToken ct)
    {
        var equipment = await _db.Equipment
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == equipmentId, ct);

        if (equipment == null)
            return PredictionEvidence.Empty(equipmentId);

        var threshold = await _db.EquipmentModelThresholds
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.EquipmentModel == equipment.Model, ct);

        var cutoff = DateTime.UtcNow.AddDays(-30);
        var telemetry = await _db.TelemetryEvents
            .AsNoTracking()
            .Where(t => t.EquipmentId == equipmentId && t.Timestamp >= cutoff)
            .ToListAsync(ct);

        if (telemetry.Count < 3)
        {
            return new PredictionEvidence
            {
                EquipmentId = equipmentId,
                ConfidenceScore = 0.3m,
                PredictedIssue = "Insufficient data",
                RecommendedAction = "Collect more telemetry data before analysis",
                Priority = "Low",
                IsColdStart = true,
                Factors = new[] { "cold-start: fewer than 30 days of telemetry" }
            };
        }

        var factors = new List<string>();
        decimal score = 0.5m;

        // Temperature analysis
        var temps = telemetry.Where(t => t.Temperature > 0).Select(t => (decimal)t.Temperature).ToList();
        if (temps.Count > 0 && threshold != null)
        {
            var avgTemp = temps.Average();
            var maxTemp = temps.Max();
            var tempThreshold = (decimal)threshold.MaxTemperature;

            if (maxTemp > tempThreshold)
            {
                score += 0.2m;
                factors.Add($"Temperature exceeded threshold: {maxTemp:F1}°C > {tempThreshold:F1}°C");
            }
            if (avgTemp > tempThreshold * 0.9m)
            {
                score += 0.1m;
                factors.Add($"Average temperature trending high: {avgTemp:F1}°C (threshold: {tempThreshold:F1}°C)");
            }
        }

        // Fuel consumption analysis
        var fuelLevels = telemetry.Where(t => t.FuelLevel > 0).Select(t => (decimal)t.FuelLevel).ToList();
        if (fuelLevels.Count > 1 && threshold != null)
        {
            var fuelVariance = fuelLevels.Max() - fuelLevels.Min();
            if (fuelVariance > 50)
            {
                score += 0.1m;
                factors.Add($"High fuel level variance: {fuelVariance:F1}%");
            }
        }

        // Equipment status analysis
        if (equipment.Status is "NeedsService" or "OutOfService")
        {
            score += 0.15m;
            factors.Add($"Equipment status: {equipment.Status}");
        }

        // Service history
        if (equipment.LastServiceDate.HasValue)
        {
            var daysSinceService = (DateTime.UtcNow - equipment.LastServiceDate.Value).TotalDays;
            if (daysSinceService > 90)
            {
                score += 0.1m;
                factors.Add($"Last service: {daysSinceService:F0} days ago");
            }
        }

        score = Math.Min(score, 0.99m);

        var priority = score switch
        {
            >= 0.8m => "High",
            >= 0.5m => "Medium",
            _ => "Low"
        };

        var predictedIssue = DetermineIssue(factors, equipment);

        return new PredictionEvidence
        {
            EquipmentId = equipmentId,
            ConfidenceScore = score,
            PredictedIssue = predictedIssue,
            RecommendedAction = DetermineAction(predictedIssue, priority),
            Priority = priority,
            IsColdStart = false,
            Factors = factors.ToArray()
        };
    }

    private static string DetermineIssue(List<string> factors, Equipment equipment)
    {
        if (factors.Any(f => f.Contains("Temperature exceeded")))
            return "Engine overheating risk";
        if (factors.Any(f => f.Contains("fuel level variance")))
            return "Abnormal fuel consumption pattern";
        if (equipment.Status == "OutOfService")
            return "Equipment requires immediate maintenance";
        if (equipment.Status == "NeedsService")
            return "Scheduled maintenance overdue";
        return "General wear monitoring recommended";
    }

    private static string DetermineAction(string issue, string priority)
    {
        return issue switch
        {
            "Engine overheating risk" => "Inspect cooling system, check coolant levels and radiator condition within 7 days",
            "Abnormal fuel consumption pattern" => "Check fuel system for leaks, inspect injectors and fuel filters",
            "Equipment requires immediate maintenance" => "Schedule emergency maintenance and remove from service rotation",
            "Scheduled maintenance overdue" => "Schedule preventive maintenance at next available window",
            _ => $"Continue monitoring. Next review recommended based on {priority.ToLower()} priority schedule"
        };
    }
}

public class PredictionEvidence
{
    public Guid EquipmentId { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string PredictedIssue { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public bool IsColdStart { get; set; }
    public string[] Factors { get; set; } = Array.Empty<string>();

    public static PredictionEvidence Empty(Guid equipmentId) => new()
    {
        EquipmentId = equipmentId,
        ConfidenceScore = 0,
        PredictedIssue = "Equipment not found",
        Priority = "Low",
        IsColdStart = true,
        Factors = new[] { "Equipment not found in database" }
    };
}
