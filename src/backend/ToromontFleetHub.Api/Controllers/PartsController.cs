using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/parts")]
[Authorize]
public class PartsController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<PartsController> _logger;

    public PartsController(FleetHubDbContext db, ITenantContext tenant, ILogger<PartsController> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<Part>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? category = null,
        [FromQuery] string? compatibility = null,
        [FromQuery] string? availability = null,
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        var query = _db.Parts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(compatibility))
            query = query.Where(p => p.CompatibleModels.Contains(compatibility));

        if (!string.IsNullOrWhiteSpace(availability))
            query = query.Where(p => p.Availability == availability);

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var desc = sort.StartsWith("-");
            var field = desc ? sort[1..] : sort;
            query = field.ToLowerInvariant() switch
            {
                "name" => desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "price" => desc ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "partnumber" => desc ? query.OrderByDescending(p => p.PartNumber) : query.OrderBy(p => p.PartNumber),
                _ => query.OrderBy(p => p.Name)
            };
        }
        else
        {
            query = query.OrderBy(p => p.Name);
        }

        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(take).ToListAsync(ct);

        return Ok(new PaginatedResponse<Part>
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Part>> GetById(Guid id, CancellationToken ct)
    {
        var part = await _db.Parts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (part is null)
            return NotFound();

        return Ok(part);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<Part>>> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { Error = "Query parameter 'q' is required." });

        // AI-style natural language search: tokenize query and match against multiple fields
        var tokens = q.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var query = _db.Parts.AsNoTracking();

        foreach (var token in tokens)
        {
            var t = token; // capture
            query = query.Where(p =>
                p.Name.ToLower().Contains(t) ||
                p.Description.ToLower().Contains(t) ||
                p.PartNumber.ToLower().Contains(t) ||
                p.Category.ToLower().Contains(t) ||
                p.CompatibleModels.ToLower().Contains(t));
        }

        var results = await query.Take(50).ToListAsync(ct);
        return Ok(results);
    }
}
