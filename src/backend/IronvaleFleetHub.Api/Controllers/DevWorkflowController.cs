using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.Features.Workflows;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/dev/workflows")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class DevWorkflowController : ControllerBase
{
    private readonly IWorkflowEngine _engine;
    private readonly IHostEnvironment _env;

    public DevWorkflowController(IWorkflowEngine engine, IHostEnvironment env)
    {
        _engine = engine;
        _env = env;
    }

    [HttpPost("{name}/trigger")]
    public async Task<IActionResult> Trigger(string name, CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        await _engine.TriggerAsync(name, ct);
        return Ok(new { triggered = name, timestamp = DateTime.UtcNow });
    }

    [HttpGet]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var statuses = await _engine.GetStatusAsync(ct);
        return Ok(statuses);
    }

    [HttpGet("runs")]
    public async Task<IActionResult> GetRuns(
        [FromQuery] string? name,
        [FromServices] Data.FleetHubDbContext db,
        CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var query = db.WorkflowRunRecords.AsQueryable();
        if (!string.IsNullOrEmpty(name))
            query = query.Where(r => r.WorkflowName == name);

        var runs = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                query.OrderByDescending(r => r.StartedAtUtc).Take(50),
                ct);

        return Ok(runs);
    }
}
