using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IronvaleFleetHub.Functions.Services;

public class DapperTelemetryRepository : ITelemetryRepository
{
    private readonly string? _connectionString;
    private readonly ILogger<DapperTelemetryRepository> _logger;

    public DapperTelemetryRepository(IConfiguration configuration, ILogger<DapperTelemetryRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _logger = logger;
    }

    public async Task<(Guid eventId, Guid organizationId)> InsertTelemetryEventAsync(
        Guid equipmentId,
        string eventType,
        DateTime timestamp,
        double engineHours,
        double fuelLevel,
        double temperature,
        double latitude,
        double longitude,
        string? payload,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("Database connection string is not configured.");

        var eventId = Guid.NewGuid();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        // Look up OrganizationId from Equipment
        var organizationId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            "SELECT OrganizationId FROM Equipment WHERE Id = @EquipmentId",
            new { EquipmentId = equipmentId });

        if (organizationId is null)
            throw new ArgumentException($"Equipment {equipmentId} not found.");

        await connection.ExecuteAsync(
            @"INSERT INTO TelemetryEvents (Id, EquipmentId, OrganizationId, EventType, [Timestamp], EngineHours, FuelLevel, Temperature, Latitude, Longitude, Payload)
              VALUES (@Id, @EquipmentId, @OrganizationId, @EventType, @Timestamp, @EngineHours, @FuelLevel, @Temperature, @Latitude, @Longitude, @Payload)",
            new
            {
                Id = eventId,
                EquipmentId = equipmentId,
                OrganizationId = organizationId.Value,
                EventType = eventType,
                Timestamp = timestamp,
                EngineHours = engineHours,
                FuelLevel = fuelLevel,
                Temperature = temperature,
                Latitude = latitude,
                Longitude = longitude,
                Payload = payload
            });

        _logger.LogInformation("Telemetry event {EventId} persisted for equipment {EquipmentId}", eventId, equipmentId);

        return (eventId, organizationId.Value);
    }

    public async Task EvaluateAlertAsync(
        Guid telemetryEventId,
        Guid equipmentId,
        Guid organizationId,
        double temperature,
        double fuelLevel,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("Database connection string is not configured.");

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        // Look up equipment make+model for threshold matching
        var equipment = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT Make, Model FROM Equipment WHERE Id = @EquipmentId",
            new { EquipmentId = equipmentId });

        if (equipment is null) return;

        string equipmentModel = $"{equipment.Make} {equipment.Model}";

        var threshold = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT MaxTemperature, MaxFuelConsumptionRate, MaxOperatingHoursPerDay FROM EquipmentModelThresholds WHERE EquipmentModel = @EquipmentModel",
            new { EquipmentModel = equipmentModel });

        if (threshold is null) return;

        // Temperature check
        if (temperature > (double)threshold.MaxTemperature)
        {
            var severity = temperature > (double)threshold.MaxTemperature * 1.1 ? "Critical" : "High";

            await InsertAlertIfNotDuplicateAsync(connection,
                telemetryEventId, organizationId, equipmentId,
                "TemperatureExceeded", severity,
                $"Temperature {temperature:F1}C exceeds threshold {(double)threshold.MaxTemperature:F1}C");
        }

        // Low fuel check
        if (fuelLevel < 10)
        {
            var severity = fuelLevel < 5 ? "High" : "Medium";

            await InsertAlertIfNotDuplicateAsync(connection,
                telemetryEventId, organizationId, equipmentId,
                "FuelLevel", severity,
                $"Fuel level critically low at {fuelLevel:F1}%");
        }
    }

    private static async Task InsertAlertIfNotDuplicateAsync(
        SqlConnection connection,
        Guid telemetryEventId,
        Guid organizationId,
        Guid equipmentId,
        string alertType,
        string severity,
        string message)
    {
        // Idempotent: skip if alert for this event+type already exists
        var exists = await connection.QuerySingleAsync<int>(
            "SELECT COUNT(1) FROM Alerts WHERE SourceTelemetryEventId = @EventId AND AlertType = @AlertType",
            new { EventId = telemetryEventId, AlertType = alertType });

        if (exists > 0) return;

        await connection.ExecuteAsync(
            @"INSERT INTO Alerts (Id, OrganizationId, EquipmentId, AlertType, Severity, Message, Status, CreatedAt, SourceTelemetryEventId)
              VALUES (@Id, @OrganizationId, @EquipmentId, @AlertType, @Severity, @Message, 'Active', @CreatedAt, @SourceTelemetryEventId)",
            new
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                EquipmentId = equipmentId,
                AlertType = alertType,
                Severity = severity,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                SourceTelemetryEventId = telemetryEventId
            });
    }
}
