using IronvaleFleetHub.Functions.Models;

namespace IronvaleFleetHub.Functions.Services;

public interface IDeadLetterService
{
    Task RecordAsync(TelemetryDeadLetterEntry entry, CancellationToken ct = default);
}
