using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Notifications.Commands;

public record UpdateNotificationPreferencesCommand(List<NotificationPreference> Preferences)
    : IRequest<List<NotificationPreference>>;

public class UpdateNotificationPreferencesCommandHandler
    : IRequestHandler<UpdateNotificationPreferencesCommand, List<NotificationPreference>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdateNotificationPreferencesCommandHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<NotificationPreference>> Handle(
        UpdateNotificationPreferencesCommand request, CancellationToken ct)
    {
        var existingPrefs = await _db.NotificationPreferences
            .Where(p => p.UserId == _tenant.UserId)
            .ToListAsync(ct);

        foreach (var pref in request.Preferences)
        {
            var existing = existingPrefs.FirstOrDefault(e => e.NotificationType == pref.NotificationType);
            if (existing is not null)
            {
                existing.EmailEnabled = pref.EmailEnabled;
                existing.SmsEnabled = pref.SmsEnabled;
            }
            else
            {
                _db.NotificationPreferences.Add(new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = _tenant.UserId,
                    NotificationType = pref.NotificationType,
                    EmailEnabled = pref.EmailEnabled,
                    SmsEnabled = pref.SmsEnabled
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        return await _db.NotificationPreferences
            .Where(p => p.UserId == _tenant.UserId)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
