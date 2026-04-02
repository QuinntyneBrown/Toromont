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
}
