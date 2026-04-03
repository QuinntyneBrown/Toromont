using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Services;

public sealed class NotificationPreferenceEvaluator
{
    private readonly FleetHubDbContext _db;

    public NotificationPreferenceEvaluator(FleetHubDbContext db)
    {
        _db = db;
    }

    public async Task<(bool EmailEnabled, bool SmsEnabled)> EvaluateAsync(
        Guid userId,
        string eventType,
        CancellationToken ct = default)
    {
        var pref = await _db.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == eventType, ct);

        if (pref is null)
            return (false, false);

        return (pref.EmailEnabled, pref.SmsEnabled);
    }
}
