using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IronvaleFleetHub.Functions.Models;

namespace IronvaleFleetHub.Functions.Services;

public class DeadLetterService : IDeadLetterService
{
    private readonly string? _connectionString;
    private readonly ILogger<DeadLetterService> _logger;

    public DeadLetterService(IConfiguration configuration, ILogger<DeadLetterService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _logger = logger;
    }

    public async Task RecordAsync(TelemetryDeadLetterEntry entry, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            _logger.LogError("Cannot record dead letter entry: connection string not configured. Entry: {EntryId}", entry.Id);
            return;
        }

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await connection.ExecuteAsync(
            @"INSERT INTO TelemetryDeadLetterEntries (Id, EquipmentId, OriginalPayload, ErrorMessage, FailedAt, RetryCount, IsReprocessed)
              VALUES (@Id, @EquipmentId, @OriginalPayload, @ErrorMessage, @FailedAt, @RetryCount, @IsReprocessed)",
            new
            {
                entry.Id,
                entry.EquipmentId,
                entry.OriginalPayload,
                entry.ErrorMessage,
                entry.FailedAt,
                entry.RetryCount,
                entry.IsReprocessed
            });

        _logger.LogWarning("Telemetry event dead-lettered for equipment {EquipmentId}: {Error}",
            entry.EquipmentId, entry.ErrorMessage);
    }
}
