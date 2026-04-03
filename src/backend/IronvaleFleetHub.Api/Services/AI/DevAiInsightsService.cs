using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using IronvaleFleetHub.Api.Data;

namespace IronvaleFleetHub.Api.Services.AI;

public class DevAiInsightsService : IAiInsightsService
{
    private readonly FleetHubDbContext _db;
    private readonly RuleBasedPredictionEngine _engine;
    private readonly NarrativeFormatter _formatter;
    private readonly DevAiOptions _options;
    private readonly ILogger<DevAiInsightsService> _logger;
    private static readonly MemoryCache Cache = new(new MemoryCacheOptions());

    public DevAiInsightsService(
        FleetHubDbContext db,
        RuleBasedPredictionEngine engine,
        NarrativeFormatter formatter,
        IOptions<DevAiOptions> options,
        ILogger<DevAiInsightsService> logger)
    {
        _db = db;
        _engine = engine;
        _formatter = formatter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MaintenancePrediction> PredictMaintenanceAsync(Guid equipmentId, CancellationToken ct)
    {
        var cacheKey = $"prediction:{equipmentId}";
        if (Cache.TryGetValue(cacheKey, out MaintenancePrediction? cached) && cached != null)
            return cached;

        // Check for seeded scenario override
        var scenario = await _db.AiScenarioRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.EquipmentId == equipmentId && s.ScenarioType == "Prediction", ct);

        MaintenancePrediction result;
        if (scenario != null)
        {
            _logger.LogDebug("Using seeded scenario for equipment {EquipmentId}", equipmentId);
            result = new MaintenancePrediction
            {
                EquipmentId = equipmentId,
                ConfidenceScore = scenario.ConfidenceScore,
                PredictedIssue = scenario.PredictedIssue,
                RecommendedAction = scenario.RecommendedAction,
                Priority = scenario.Priority,
                Explanation = scenario.Explanation
            };
        }
        else
        {
            var evidence = await _engine.EvaluateAsync(equipmentId, ct);
            result = new MaintenancePrediction
            {
                EquipmentId = equipmentId,
                ConfidenceScore = evidence.ConfidenceScore,
                PredictedIssue = evidence.PredictedIssue,
                RecommendedAction = evidence.RecommendedAction,
                Priority = evidence.Priority,
                Explanation = _formatter.FormatPrediction(evidence)
            };
        }

        Cache.Set(cacheKey, result, TimeSpan.FromSeconds(_options.CacheSeconds));
        return result;
    }

    public async Task<IReadOnlyList<AnomalyInsight>> DetectAnomaliesAsync(Guid equipmentId, CancellationToken ct)
    {
        var cacheKey = $"anomalies:{equipmentId}";
        if (Cache.TryGetValue(cacheKey, out IReadOnlyList<AnomalyInsight>? cached) && cached != null)
            return cached;

        var cutoff = DateTime.UtcNow.AddDays(-7);
        var telemetry = await _db.TelemetryEvents
            .AsNoTracking()
            .Where(t => t.EquipmentId == equipmentId && t.Timestamp >= cutoff)
            .ToListAsync(ct);

        var insights = new List<AnomalyInsight>();

        if (telemetry.Count < 2)
        {
            Cache.Set(cacheKey, (IReadOnlyList<AnomalyInsight>)insights, TimeSpan.FromSeconds(_options.CacheSeconds));
            return insights;
        }

        // Temperature anomaly detection
        var temps = telemetry.Where(t => t.Temperature > 0).Select(t => (decimal)t.Temperature).ToList();
        if (temps.Count >= 2)
        {
            var mean = temps.Average();
            var stdDev = (decimal)Math.Sqrt(temps.Select(t => Math.Pow((double)(t - mean), 2)).Average());
            var latest = temps.Last();

            if (stdDev > 0 && Math.Abs(latest - mean) > 2 * stdDev)
            {
                var deviation = mean != 0 ? Math.Abs((latest - mean) / mean * 100) : 0;
                var severity = deviation > 30 ? "High" : deviation > 15 ? "Medium" : "Low";
                insights.Add(new AnomalyInsight
                {
                    Metric = "Engine Temperature",
                    Baseline = mean,
                    CurrentValue = latest,
                    DeviationPercent = deviation,
                    Severity = severity,
                    Explanation = _formatter.FormatAnomaly("Engine Temperature", mean, latest, deviation, severity)
                });
            }
        }

        // Fuel level anomaly detection
        var fuels = telemetry.Where(t => t.FuelLevel > 0).Select(t => (decimal)t.FuelLevel).ToList();
        if (fuels.Count >= 2)
        {
            var baseline = fuels.Take(fuels.Count / 2).Average();
            var recent = fuels.Skip(fuels.Count / 2).Average();

            if (baseline > 0)
            {
                var deviation = Math.Abs((recent - baseline) / baseline * 100);
                if (deviation > 30)
                {
                    var severity = deviation > 50 ? "High" : "Medium";
                    insights.Add(new AnomalyInsight
                    {
                        Metric = "Fuel Consumption",
                        Baseline = baseline,
                        CurrentValue = recent,
                        DeviationPercent = deviation,
                        Severity = severity,
                        Explanation = _formatter.FormatAnomaly("Fuel Consumption", baseline, recent, deviation, severity)
                    });
                }
            }
        }

        IReadOnlyList<AnomalyInsight> result = insights;
        Cache.Set(cacheKey, result, TimeSpan.FromSeconds(_options.CacheSeconds));
        return result;
    }
}
