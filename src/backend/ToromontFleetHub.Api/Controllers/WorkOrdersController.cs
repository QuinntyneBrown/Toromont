using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/work-orders")]
[Authorize]
public class WorkOrdersController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWorkOrderNumberGenerator _woNumberGenerator;
    private readonly ILogger<WorkOrdersController> _logger;

    public WorkOrdersController(
        FleetHubDbContext db,
        ITenantContext tenant,
        IWorkOrderNumberGenerator woNumberGenerator,
        ILogger<WorkOrdersController> logger)
    {
        _db = db;
        _tenant = tenant;
        _woNumberGenerator = woNumberGenerator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<WorkOrder>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        var query = _db.WorkOrders
            .Include(w => w.Equipment)
            .Include(w => w.AssignedTo)
            .Where(w => w.OrganizationId == _tenant.OrganizationId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(w => w.Status == status);

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var desc = sort.StartsWith("-");
            var field = desc ? sort[1..] : sort;
            query = field.ToLowerInvariant() switch
            {
                "priority" => desc ? query.OrderByDescending(w => w.Priority) : query.OrderBy(w => w.Priority),
                "requesteddate" => desc ? query.OrderByDescending(w => w.RequestedDate) : query.OrderBy(w => w.RequestedDate),
                "status" => desc ? query.OrderByDescending(w => w.Status) : query.OrderBy(w => w.Status),
                _ => query.OrderByDescending(w => w.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(w => w.CreatedAt);
        }

        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(take).ToListAsync(ct);

        return Ok(new PaginatedResponse<WorkOrder>
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
    public async Task<ActionResult<WorkOrder>> GetById(Guid id, CancellationToken ct)
    {
        var wo = await _db.WorkOrders
            .Include(w => w.Equipment)
            .Include(w => w.AssignedTo)
            .Include(w => w.History.OrderByDescending(h => h.ChangedAt))
                .ThenInclude(h => h.ChangedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == _tenant.OrganizationId, ct);

        if (wo is null)
            return NotFound();

        return Ok(wo);
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrder>> Create([FromBody] CreateWorkOrderRequest request, CancellationToken ct)
    {
        var equipmentExists = await _db.Equipment
            .AnyAsync(e => e.Id == request.EquipmentId && e.OrganizationId == _tenant.OrganizationId, ct);

        if (!equipmentExists)
            return BadRequest(new { Error = "Equipment not found in this organization." });

        var woNumber = await _woNumberGenerator.GenerateAsync(ct);

        var wo = new WorkOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = _tenant.OrganizationId,
            WorkOrderNumber = woNumber,
            EquipmentId = request.EquipmentId,
            ServiceType = request.ServiceType,
            Priority = request.Priority,
            Status = "Open",
            Description = request.Description,
            RequestedDate = request.RequestedDate,
            AssignedToUserId = request.AssignedToUserId,
            CreatedAt = DateTime.UtcNow
        };

        wo.History.Add(new WorkOrderHistory
        {
            Id = Guid.NewGuid(),
            WorkOrderId = wo.Id,
            PreviousStatus = string.Empty,
            NewStatus = "Open",
            Notes = "Work order created.",
            ChangedByUserId = _tenant.UserId,
            ChangedAt = DateTime.UtcNow
        });

        _db.WorkOrders.Add(wo);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Work order {WONumber} created by user {UserId}", woNumber, _tenant.UserId);
        return CreatedAtAction(nameof(GetById), new { id = wo.Id }, wo);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<WorkOrder>> UpdateStatus(Guid id, [FromBody] StatusUpdateRequest request, CancellationToken ct)
    {
        var wo = await _db.WorkOrders
            .Include(w => w.History)
            .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == _tenant.OrganizationId, ct);

        if (wo is null)
            return NotFound();

        var allowedTransitions = GetAllowedTransitions(wo.Status, _tenant.UserRole);
        if (!allowedTransitions.Contains(request.Status))
            return BadRequest(new { Error = $"Cannot transition from '{wo.Status}' to '{request.Status}' with role '{_tenant.UserRole}'." });

        var previousStatus = wo.Status;
        wo.Status = request.Status;

        if (request.Status == "Completed")
            wo.CompletedDate = DateTime.UtcNow;

        var history = new WorkOrderHistory
        {
            Id = Guid.NewGuid(),
            WorkOrderId = wo.Id,
            PreviousStatus = previousStatus,
            NewStatus = request.Status,
            Notes = request.Notes,
            ChangedByUserId = _tenant.UserId,
            ChangedAt = DateTime.UtcNow
        };
        _db.WorkOrderHistories.Add(history);

        await _db.SaveChangesAsync(ct);

        // Reload with history for response
        var updated = await _db.WorkOrders
            .Include(w => w.History)
            .Include(w => w.Equipment)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        return Ok(updated);
    }

    [HttpGet("calendar")]
    public async Task<ActionResult> GetCalendar(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        CancellationToken ct)
    {
        var items = await _db.WorkOrders
            .Include(w => w.Equipment)
            .Where(w => w.OrganizationId == _tenant.OrganizationId
                && w.RequestedDate >= start
                && w.RequestedDate <= end)
            .AsNoTracking()
            .Select(w => new
            {
                w.Id,
                Title = w.WorkOrderNumber + " - " + (w.Equipment != null ? w.Equipment.Name : ""),
                Start = w.RequestedDate,
                End = w.CompletedDate ?? w.RequestedDate.AddHours(4),
                w.Status,
                w.Priority,
                w.ServiceType
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    private static List<string> GetAllowedTransitions(string currentStatus, string role)
    {
        var transitions = new Dictionary<string, List<string>>
        {
            ["Open"] = new() { "InProgress", "Cancelled" },
            ["InProgress"] = new() { "OnHold", "Completed", "Cancelled" },
            ["OnHold"] = new() { "InProgress", "Cancelled" },
            ["Completed"] = new() { "Closed" },
            ["Closed"] = new(),
            ["Cancelled"] = new()
        };

        if (!transitions.TryGetValue(currentStatus, out var allowed))
            return new List<string>();

        // Only Admin and FleetManager can reopen or cancel
        if (role is not ("Admin" or "FleetManager"))
        {
            allowed = allowed.Where(s => s is not ("Open" or "Cancelled")).ToList();
        }

        return allowed;
    }
}
