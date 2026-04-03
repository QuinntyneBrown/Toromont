using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IronvaleFleetHub.Functions.Services;
using IronvaleFleetHub.Telemetry;

namespace IronvaleFleetHub.Functions.Functions;

public class TelemetryAlertEvaluationFunction
{
    private readonly ITelemetryRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelemetryAlertEvaluationFunction> _logger;

    public TelemetryAlertEvaluationFunction(
        ITelemetryRepository repository,
        IConfiguration configuration,
        ILogger<TelemetryAlertEvaluationFunction> logger)
    {
        _repository = repository;
        _configuration = configuration;
        _logger = logger;
    }

    [Function("TelemetryAlertEvaluation")]
    public async Task Run(
        [QueueTrigger("telemetry-alert-evaluation", Connection = "AzureWebJobsStorage")]
        string messageJson,
        CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<TelemetryAlertEvaluationMessage>(messageJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (message is null)
        {
            _logger.LogWarning("Received null alert evaluation message, skipping");
            return;
        }

        _logger.LogInformation(
            "Evaluating telemetry alert for event {EventId}, equipment {EquipmentId}, org {OrgId}",
            message.TelemetryEventId, message.EquipmentId, message.OrganizationId);

        try
        {
            await _repository.EvaluateAlertAsync(
                message.TelemetryEventId,
                message.EquipmentId,
                message.OrganizationId,
                message.Temperature,
                message.FuelLevel,
                ct);

            _logger.LogInformation(
                "Alert evaluation complete for event {EventId}", message.TelemetryEventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Alert evaluation failed for event {EventId}, equipment {EquipmentId}",
                message.TelemetryEventId, message.EquipmentId);
            throw; // Let the Functions runtime handle retry/dead-letter
        }
    }
}
