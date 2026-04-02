namespace IronvaleFleetHub.Functions.Services;

public interface ITelemetryRepository
{
    Task<(Guid eventId, Guid organizationId)> InsertTelemetryEventAsync(
        Guid equipmentId,
        string eventType,
        DateTime timestamp,
        double engineHours,
        double fuelLevel,
        double temperature,
        double latitude,
        double longitude,
        string? payload,
        CancellationToken ct = default);
}
