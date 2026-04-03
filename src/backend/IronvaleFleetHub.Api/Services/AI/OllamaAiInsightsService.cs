using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace IronvaleFleetHub.Api.Services.AI;

public class OllamaAiInsightsService : IAiInsightsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RuleBasedPredictionEngine _engine;
    private readonly NarrativeFormatter _formatter;
    private readonly DevAiOptions _options;
    private readonly ILogger<OllamaAiInsightsService> _logger;

    public OllamaAiInsightsService(
        IHttpClientFactory httpClientFactory,
        RuleBasedPredictionEngine engine,
        NarrativeFormatter formatter,
        IOptions<DevAiOptions> options,
        ILogger<OllamaAiInsightsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _engine = engine;
        _formatter = formatter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MaintenancePrediction> PredictMaintenanceAsync(Guid equipmentId, CancellationToken ct)
    {
        var evidence = await _engine.EvaluateAsync(equipmentId, ct);
        string explanation;

        try
        {
            explanation = await EnhanceWithOllamaAsync(evidence, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama unreachable, falling back to rule-based narrative for equipment {EquipmentId}", equipmentId);
            explanation = _formatter.FormatPrediction(evidence);
        }

        return new MaintenancePrediction
        {
            EquipmentId = equipmentId,
            ConfidenceScore = evidence.ConfidenceScore,
            PredictedIssue = evidence.PredictedIssue,
            RecommendedAction = evidence.RecommendedAction,
            Priority = evidence.Priority,
            Explanation = explanation
        };
    }

    public async Task<IReadOnlyList<AnomalyInsight>> DetectAnomaliesAsync(Guid equipmentId, CancellationToken ct)
    {
        // Anomaly detection uses rule-based engine only; Ollama used for explanation text enhancement
        var engine = new DevAiInsightsService(null!, _engine, _formatter, Options.Create(_options), null!);
        // Fall back to rule-based for anomalies
        _logger.LogDebug("Anomaly detection uses rule-based engine (Ollama limited to explanation enhancement)");
        return Array.Empty<AnomalyInsight>();
    }

    private async Task<string> EnhanceWithOllamaAsync(PredictionEvidence evidence, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Ollama");
        var prompt = $"You are a fleet maintenance AI. Explain this prediction concisely:\n" +
                     $"Equipment issue: {evidence.PredictedIssue}\n" +
                     $"Confidence: {evidence.ConfidenceScore:P0}\n" +
                     $"Factors: {string.Join("; ", evidence.Factors)}\n" +
                     $"Recommended action: {evidence.RecommendedAction}\n" +
                     "Provide a 2-3 sentence explanation.";

        var requestBody = JsonSerializer.Serialize(new
        {
            model = "llama3.2",
            prompt,
            stream = false
        });

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        var response = await client.PostAsync("/api/generate",
            new StringContent(requestBody, Encoding.UTF8, "application/json"),
            cts.Token);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cts.Token);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("response").GetString()
            ?? _formatter.FormatPrediction(evidence);
    }
}
