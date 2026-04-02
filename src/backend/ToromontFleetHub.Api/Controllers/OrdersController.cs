using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(FleetHubDbContext db, ITenantContext tenant, ILogger<OrdersController> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<PartsOrder>> SubmitOrder(CancellationToken ct)
    {
        var cartItems = await _db.CartItems
            .Include(c => c.Part)
            .Where(c => c.UserId == _tenant.UserId)
            .ToListAsync(ct);

        if (cartItems.Count == 0)
            return BadRequest(new { Error = "Cart is empty." });

        var today = DateTime.UtcNow;
        var dateStr = today.ToString("yyyyMMdd");
        var dailyCount = await _db.PartsOrders
            .CountAsync(o => o.OrderNumber.StartsWith($"ORD-{dateStr}"), ct);

        var orderNumber = $"ORD-{dateStr}-{(dailyCount + 1):D3}";

        var order = new PartsOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = _tenant.OrganizationId,
            OrderNumber = orderNumber,
            UserId = _tenant.UserId,
            Status = "Submitted",
            CreatedAt = today
        };

        decimal subtotal = 0;
        foreach (var cartItem in cartItems)
        {
            var part = cartItem.Part!;
            var lineTotal = part.Price * cartItem.Quantity;
            subtotal += lineTotal;

            order.LineItems.Add(new OrderLineItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PartId = cartItem.PartId,
                PartNumber = part.PartNumber,
                Name = part.Name,
                UnitPrice = part.Price,
                Quantity = cartItem.Quantity,
                LineTotal = lineTotal
            });
        }

        order.Subtotal = subtotal;

        _db.PartsOrders.Add(order);
        _db.CartItems.RemoveRange(cartItems);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderNumber} submitted by user {UserId}, total {Subtotal}",
            orderNumber, _tenant.UserId, subtotal);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<PartsOrder>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var query = _db.PartsOrders
            .Include(o => o.LineItems)
            .Where(o => o.UserId == _tenant.UserId && o.OrganizationId == _tenant.OrganizationId)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking();

        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(take).ToListAsync(ct);

        return Ok(new PaginatedResponse<PartsOrder>
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
    public async Task<ActionResult<PartsOrder>> GetById(Guid id, CancellationToken ct)
    {
        var order = await _db.PartsOrders
            .Include(o => o.LineItems)
            .Include(o => o.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && o.OrganizationId == _tenant.OrganizationId, ct);

        if (order is null)
            return NotFound();

        return Ok(order);
    }
}
