using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(FleetHubDbContext db, ITenantContext tenant, ILogger<NotificationsController> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<Notification>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var query = _db.Notifications
            .Where(n => n.UserId == _tenant.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .AsNoTracking();

        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(take).ToListAsync(ct);

        return Ok(new PaginatedResponse<Notification>
        {
            Items = items,
            Pagination = new PaginationInfo
            {
                Page = skip / take + 1,
                PageSize = take,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)take)
            }
        });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<Notification>> MarkAsRead(Guid id, CancellationToken ct)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == _tenant.UserId, ct);

        if (notification is null)
            return NotFound();

        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);

        return Ok(notification);
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        await _db.Notifications
            .Where(n => n.UserId == _tenant.UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

        return NoContent();
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult> GetUnreadCount(CancellationToken ct)
    {
        var count = await _db.Notifications
            .CountAsync(n => n.UserId == _tenant.UserId && !n.IsRead, ct);

        return Ok(new { UnreadCount = count });
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<List<NotificationPreference>>> GetPreferences(CancellationToken ct)
    {
        var prefs = await _db.NotificationPreferences
            .Where(p => p.UserId == _tenant.UserId)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(prefs);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<List<NotificationPreference>>> UpdatePreferences(
        [FromBody] List<NotificationPreference> preferences,
        CancellationToken ct)
    {
        var existingPrefs = await _db.NotificationPreferences
            .Where(p => p.UserId == _tenant.UserId)
            .ToListAsync(ct);

        foreach (var pref in preferences)
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

        var updated = await _db.NotificationPreferences
            .Where(p => p.UserId == _tenant.UserId)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(updated);
    }
}
