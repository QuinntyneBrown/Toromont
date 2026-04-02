using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IronvaleFleetHub.Functions.Models;
using IronvaleFleetHub.Functions.Services;

namespace IronvaleFleetHub.Functions.Functions;

public class TelemetryIngestionFunction
{
    private static readonly int[] RetryDelaysMs = { 1000, 4000, 16000 };

    private readonly ITelemetryRepository _repository;
    private readonly IDeadLetterService _deadLetterService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelemetryIngestionFunction> _logger;

    public TelemetryIngestionFunction(
        ITelemetryRepository repository,
        IDeadLetterService deadLetterService,
        IConfiguration configuration,
        ILogger<TelemetryIngestionFunction> logger)
    {
        _repository = repository;
        _deadLetterService = deadLetterService;
        _configuration = configuration;
        _logger = logger;
    }

    [Function("TelemetryIngestion")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "telemetry")] HttpRequestData req,
        CancellationToken ct)
    {
        // API Key validation - no hardcoded fallback
        if (!req.Headers.TryGetValues("X-Api-Key", out var apiKeyValues))
        {
            return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "API key is required.");
        }

        var apiKey = apiKeyValues.FirstOrDefault();
        var configuredKey = _configuration["Telemetry:ApiKey"]
            ?? throw new InvalidOperationException("Telemetry:ApiKey is not configured.");

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey != configuredKey)
        {
            return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid API key.");
        }

        // Deserialize and validate request
        TelemetryIngestionRequest? request;
        try
        {
            var body = await req.ReadAsStringAsync();
            request = JsonSerializer.Deserialize<TelemetryIngestionRequest>(body!, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in telemetry ingestion request");
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body.");
        }

        if (request is null || request.EquipmentId == Guid.Empty)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "EquipmentId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "EventType is required.");
        }

        var timestamp = request.Timestamp ?? DateTime.UtcNow;
        var payloadJson = request.Payload is not null
            ? JsonSerializer.Serialize(request.Payload)
            : null;

        // Retry loop with exponential backoff
        Exception? lastException = null;
        for (var attempt = 0; attempt <= RetryDelaysMs.Length; attempt++)
        {
            try
            {
                var (eventId, _) = await _repository.InsertTelemetryEventAsync(
                    request.EquipmentId,
                    request.EventType,
                    timestamp,
                    request.Payload?.EngineHours ?? 0,
                    request.Payload?.FuelLevel ?? 0,
                    request.Payload?.Temperature ?? 0,
                    request.Payload?.Latitude ?? 0,
                    request.Payload?.Longitude ?? 0,
                    payloadJson,
                    ct);

                var response = req.CreateResponse(HttpStatusCode.Accepted);
                await response.WriteAsJsonAsync(new { eventId, status = "accepted" }, ct);
                return response;
            }
            catch (ArgumentException ex)
            {
                // Equipment not found - don't retry
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Telemetry ingestion attempt {Attempt} failed for equipment {EquipmentId}",
                    attempt + 1, request.EquipmentId);

                if (attempt < RetryDelaysMs.Length)
                {
                    await Task.Delay(RetryDelaysMs[attempt], ct);
                }
            }
        }

        // All retries exhausted - dead letter
        _logger.LogError(lastException, "All retries exhausted for equipment {EquipmentId}. Recording dead letter.",
            request.EquipmentId);

        await _deadLetterService.RecordAsync(new TelemetryDeadLetterEntry
        {
            Id = Guid.NewGuid(),
            EquipmentId = request.EquipmentId,
            OriginalPayload = JsonSerializer.Serialize(request),
            ErrorMessage = lastException?.Message ?? "Unknown error after retries exhausted",
            FailedAt = DateTime.UtcNow,
            RetryCount = RetryDelaysMs.Length,
            IsReprocessed = false
        }, ct);

        return await CreateErrorResponse(req, HttpStatusCode.InternalServerError,
            "Telemetry ingestion failed after retries. Event has been recorded for reprocessing.");
    }

    private static async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req, HttpStatusCode statusCode, string error)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error });
        return response;
    }
}
