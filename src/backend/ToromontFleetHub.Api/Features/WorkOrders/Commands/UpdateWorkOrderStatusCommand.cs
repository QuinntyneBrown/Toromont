using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Common;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Features.WorkOrders.Notifications;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.WorkOrders.Commands;

public record UpdateWorkOrderStatusCommand(Guid Id, string Status, string? Notes) : IRequest<Result<WorkOrder>>;

public class UpdateWorkOrderStatusCommandHandler : IRequestHandler<UpdateWorkOrderStatusCommand, Result<WorkOrder>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateWorkOrderStatusCommandHandler> _logger;

    public UpdateWorkOrderStatusCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        IMediator mediator,
        ILogger<UpdateWorkOrderStatusCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<WorkOrder>> Handle(UpdateWorkOrderStatusCommand request, CancellationToken ct)
    {
        var wo = await _db.WorkOrders
            .FirstOrDefaultAsync(w => w.Id == request.Id && w.OrganizationId == _tenant.OrganizationId, ct);

        if (wo is null)
            return Result<WorkOrder>.Failure("Not found.");

        var allowedTransitions = GetAllowedTransitions(wo.Status, _tenant.UserRole);
        if (!allowedTransitions.Contains(request.Status))
            return Result<WorkOrder>.Failure(
                $"Cannot transition from '{wo.Status}' to '{request.Status}' with role '{_tenant.UserRole}'.");

        var previousStatus = wo.Status;
        wo.Status = request.Status;

        if (request.Status == "Completed")
            wo.CompletedDate = DateTime.UtcNow;

        _db.WorkOrderHistories.Add(new WorkOrderHistory
        {
            Id = Guid.NewGuid(),
            WorkOrderId = wo.Id,
            PreviousStatus = previousStatus,
            NewStatus = request.Status,
            Notes = request.Notes,
            ChangedByUserId = _tenant.UserId,
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        var updated = await _db.WorkOrders
            .Include(w => w.History)
            .Include(w => w.Equipment)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.Id, ct);

        try
        {
            await _mediator.Publish(new WorkOrderStatusChangedNotification(
                wo.Id, wo.WorkOrderNumber, previousStatus, request.Status, _tenant.OrganizationId), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WorkOrderStatusChangedNotification delivery failed for work order {Id}", request.Id);
        }

        return Result<WorkOrder>.Success(updated!);
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

        if (role is not ("Admin" or "FleetManager"))
        {
            allowed = allowed.Where(s => s is not ("Open" or "Cancelled")).ToList();
        }

        return allowed;
    }
}
