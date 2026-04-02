using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Services;

public interface IAlertEvaluatorService
{
    Task EvaluateAsync(TelemetryEvent telemetryEvent);
}
