using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Services;

public sealed class ConsoleSmsChannel : ISmsChannel
{
    private readonly FleetHubDbContext _db;
    private readonly ILogger<ConsoleSmsChannel> _logger;

    public ConsoleSmsChannel(FleetHubDbContext db, ILogger<ConsoleSmsChannel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DeliveryAttemptResult> SendAsync(SmsDeliveryRequest request, CancellationToken ct = default)
    {
        try
        {
            // Log to console with structured fields
            var preview = request.Message.Length > 80
                ? request.Message[..80] + "..."
                : request.Message;

            _logger.LogInformation(
                "📱 Dev SMS → {Recipient}: {Preview}",
                request.To, preview);

            // Persist for inspection via DevNotificationController
            var record = new DevSmsMessageRecord
            {
                Id = Guid.NewGuid(),
                Recipient = request.To,
                Message = request.Message,
                EventType = "notification",
                CreatedAt = DateTime.UtcNow
            };

            _db.DevSmsMessageRecords.Add(record);
            await _db.SaveChangesAsync(ct);

            return new DeliveryAttemptResult(true, "sms", "console");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dev SMS console sink failed for {Recipient}", request.To);
            return new DeliveryAttemptResult(false, "sms", "console", ex.Message);
        }
    }
}
