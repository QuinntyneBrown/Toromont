using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Workflows;

public class WorkOrderEscalationWorkflow : IWorkflowDefinition
{
    public string Name => "WorkOrderEscalationWorkflow";
    public TimeSpan PollInterval => TimeSpan.FromMinutes(30);

    public bool Enabled(DevWorkflowOptions options) => options.EnableWorkOrderEscalation;

    public async Task ExecuteAsync(WorkflowExecutionContext context, CancellationToken ct)
    {
        var db = context.Services.GetRequiredService<FleetHubDbContext>();
        var notificationService = context.Services.GetRequiredService<INotificationDispatchService>();
        var escalationService = context.Services.GetRequiredService<EscalationPolicyService>();
        var logger = context.Services.GetRequiredService<ILogger<WorkOrderEscalationWorkflow>>();

        var overdueThreshold = DateTime.UtcNow.AddHours(-48);

        var overdueOrders = await db.WorkOrders
            .IgnoreQueryFilters()
            .Where(wo => (wo.Status == "Open" || wo.Status == "InProgress")
                && wo.CreatedAt < overdueThreshold)
            .ToListAsync(ct);

        var actionsCreated = 0;
        var actionsSkipped = 0;

        foreach (var workOrder in overdueOrders)
        {
            var newPriority = escalationService.GetNextPriority(workOrder.Priority);
            if (newPriority == null)
            {
                actionsSkipped++;
                continue; // Already at max priority
            }

            var dispatchKey = $"work-order-escalation:{workOrder.Id}:{newPriority}";
            var exists = await db.WorkflowDispatchRecords
                .AnyAsync(d => d.DispatchKey == dispatchKey, ct);

            if (exists)
            {
                actionsSkipped++;
                continue;
            }

            // Escalate priority
            workOrder.Priority = newPriority;

            // Create dispatch record
            db.WorkflowDispatchRecords.Add(new WorkflowDispatchRecord
            {
                Id = Guid.NewGuid(),
                DispatchKey = dispatchKey,
                WorkflowName = Name,
                CreatedAtUtc = DateTime.UtcNow,
                EntityType = "WorkOrder",
                EntityId = workOrder.Id
            });

            // Notify assigned user
            if (workOrder.AssignedToUserId.HasValue)
            {
                await notificationService.DispatchAsync(
                    workOrder.AssignedToUserId.Value,
                    "Escalation",
                    $"Work Order Escalated: {workOrder.WorkOrderNumber}",
                    $"Work order {workOrder.WorkOrderNumber} has been overdue for more than 48 hours. " +
                    $"Priority escalated to {newPriority}.",
                    "WorkOrder",
                    workOrder.Id,
                    ct);
            }

            actionsCreated++;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "WorkOrderEscalationWorkflow: {Overdue} overdue orders, {Created} escalated, {Skipped} skipped (correlation: {CorrelationId})",
            overdueOrders.Count, actionsCreated, actionsSkipped, context.CorrelationId);
    }
}

public class EscalationPolicyService
{
    private static readonly string[] PriorityLevels = { "Low", "Medium", "High", "Critical" };

    public string? GetNextPriority(string currentPriority)
    {
        var currentIndex = Array.IndexOf(PriorityLevels, currentPriority);
        if (currentIndex < 0 || currentIndex >= PriorityLevels.Length - 1)
            return null; // Already at max or unknown priority
        return PriorityLevels[currentIndex + 1];
    }
}
