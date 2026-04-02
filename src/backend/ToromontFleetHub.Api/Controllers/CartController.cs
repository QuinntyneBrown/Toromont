using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<CartController> _logger;

    public CartController(FleetHubDbContext db, ITenantContext tenant, ILogger<CartController> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CartItem>>> GetCart(CancellationToken ct)
    {
        var items = await _db.CartItems
            .Include(c => c.Part)
            .Where(c => c.UserId == _tenant.UserId)
            .AsNoTracking()
            .OrderByDescending(c => c.AddedAt)
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartItem>> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        var part = await _db.Parts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PartId, ct);
        if (part is null)
            return BadRequest(new { Error = "Part not found." });

        var existing = await _db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == _tenant.UserId && c.PartId == request.PartId, ct);

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
            await _db.SaveChangesAsync(ct);
            return Ok(existing);
        }

        var item = new CartItem
        {
            Id = Guid.NewGuid(),
            UserId = _tenant.UserId,
            PartId = request.PartId,
            Quantity = request.Quantity > 0 ? request.Quantity : 1,
            AddedAt = DateTime.UtcNow
        };

        _db.CartItems.Add(item);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetCart), item);
    }

    [HttpPut("items/{id:guid}")]
    public async Task<ActionResult<CartItem>> UpdateItem(Guid id, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        var item = await _db.CartItems
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == _tenant.UserId, ct);

        if (item is null)
            return NotFound();

        if (request.Quantity <= 0)
            return BadRequest(new { Error = "Quantity must be greater than zero." });

        item.Quantity = request.Quantity;
        await _db.SaveChangesAsync(ct);

        return Ok(item);
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, CancellationToken ct)
    {
        var item = await _db.CartItems
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == _tenant.UserId, ct);

        if (item is null)
            return NotFound();

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
