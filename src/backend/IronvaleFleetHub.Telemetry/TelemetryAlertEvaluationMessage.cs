namespace IronvaleFleetHub.Telemetry;

/// <summary>
/// Message published to the telemetry-alert-evaluation queue after a telemetry event
/// is persisted. Consumed by TelemetryAlertEvaluationFunction to determine whether
/// the reading exceeds configured thresholds.
/// </summary>
public sealed record TelemetryAlertEvaluationMessage(
    Guid TelemetryEventId,
    Guid EquipmentId,
    Guid OrganizationId,
    DateTime Timestamp,
    string EventType,
    double Temperature,
    double FuelLevel,
    double EngineHours);
