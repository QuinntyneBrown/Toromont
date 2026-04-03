using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/dev/notifications")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class DevNotificationController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly IHostEnvironment _env;

    public DevNotificationController(FleetHubDbContext db, IHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("sms")]
    public async Task<IActionResult> GetSmsMessages([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var messages = await _db.DevSmsMessageRecords
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return Ok(messages);
    }

    [HttpGet("deliveries")]
    public async Task<IActionResult> GetDeliveryAttempts([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var attempts = await _db.DeliveryAttemptRecords
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return Ok(attempts);
    }
}
