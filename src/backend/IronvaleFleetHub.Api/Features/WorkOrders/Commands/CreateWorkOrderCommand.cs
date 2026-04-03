using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Features.WorkOrders.Notifications;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.WorkOrders.Commands;

public record CreateWorkOrderCommand(
    Guid EquipmentId,
    string ServiceType,
    string Priority,
    string Description,
    DateTime RequestedDate,
    Guid? AssignedToUserId
) : IRequest<Result<WorkOrder>>;

public class CreateWorkOrderCommandHandler : IRequestHandler<CreateWorkOrderCommand, Result<WorkOrder>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWorkOrderNumberGenerator _woNumberGenerator;
    private readonly IMediator _mediator;
    private readonly ILogger<CreateWorkOrderCommandHandler> _logger;

    public CreateWorkOrderCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        IWorkOrderNumberGenerator woNumberGenerator,
        IMediator mediator,
        ILogger<CreateWorkOrderCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _woNumberGenerator = woNumberGenerator;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<WorkOrder>> Handle(CreateWorkOrderCommand request, CancellationToken ct)
    {
        var equipmentExists = await _db.Equipment
            .AnyAsync(e => e.Id == request.EquipmentId && e.OrganizationId == _tenant.OrganizationId, ct);

        if (!equipmentExists)
            return Result<WorkOrder>.Failure("Equipment not found in this organization.");

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

        try
        {
            await _mediator.Publish(new WorkOrderCreatedNotification(
                wo.Id, wo.WorkOrderNumber, wo.AssignedToUserId, _tenant.OrganizationId), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WorkOrderCreatedNotification delivery failed for work order {WONumber}", woNumber);
        }

        return Result<WorkOrder>.Success(wo);
    }
}

public class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        RuleFor(x => x.EquipmentId).NotEmpty();
        RuleFor(x => x.ServiceType).NotEmpty();
        RuleFor(x => x.Priority).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.RequestedDate).NotEmpty();
    }
}
