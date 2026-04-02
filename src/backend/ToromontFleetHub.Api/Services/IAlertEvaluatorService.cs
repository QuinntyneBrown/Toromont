using ToromontFleetHub.Api.Models;

namespace ToromontFleetHub.Api.Services;

public interface IAlertEvaluatorService
{
    Task EvaluateAsync(TelemetryEvent telemetryEvent);
}
