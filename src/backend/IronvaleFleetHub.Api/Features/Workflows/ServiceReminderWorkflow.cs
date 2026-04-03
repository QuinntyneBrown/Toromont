using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Workflows;

public class ServiceReminderWorkflow : IWorkflowDefinition
{
    public string Name => "ServiceReminderWorkflow";
    public TimeSpan PollInterval => TimeSpan.FromMinutes(15);

    public bool Enabled(DevWorkflowOptions options) => options.EnableServiceReminders;

    public async Task ExecuteAsync(WorkflowExecutionContext context, CancellationToken ct)
    {
        var db = context.Services.GetRequiredService<FleetHubDbContext>();
        var notificationService = context.Services.GetRequiredService<INotificationDispatchService>();
        var queryService = context.Services.GetRequiredService<ReminderCandidateQueryService>();
        var logger = context.Services.GetRequiredService<ILogger<ServiceReminderWorkflow>>();

        var candidates = await queryService.GetReminderCandidatesAsync(ct);
        var actionsCreated = 0;
        var actionsSkipped = 0;

        foreach (var candidate in candidates)
        {
            foreach (var offsetDays in new[] { 7, 3, 1 })
            {
                if (candidate.DaysUntilDue > offsetDays) continue;

                var dispatchKey = $"service-reminder:{candidate.WorkOrderId}:{offsetDays}d";

                var exists = await db.WorkflowDispatchRecords
                    .AnyAsync(d => d.DispatchKey == dispatchKey, ct);

                if (exists)
                {
                    actionsSkipped++;
                    continue;
                }

                // Dispatch notification
                if (candidate.AssignedToUserId.HasValue)
                {
                    await notificationService.DispatchAsync(
                        candidate.AssignedToUserId.Value,
                        "ServiceDue",
                        $"Service Reminder: {candidate.EquipmentName}",
                        $"Work order {candidate.WorkOrderNumber} is due in {candidate.DaysUntilDue} day(s). " +
                        $"Service type: {candidate.ServiceType}.",
                        "WorkOrder",
                        candidate.WorkOrderId,
                        ct);
                }

                db.WorkflowDispatchRecords.Add(new WorkflowDispatchRecord
                {
                    Id = Guid.NewGuid(),
                    DispatchKey = dispatchKey,
                    WorkflowName = Name,
                    CreatedAtUtc = DateTime.UtcNow,
                    EntityType = "WorkOrder",
                    EntityId = candidate.WorkOrderId
                });

                actionsCreated++;
                break; // Only one reminder per work order per run
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "ServiceReminderWorkflow: {Candidates} candidates, {Created} actions created, {Skipped} skipped (correlation: {CorrelationId})",
            candidates.Count, actionsCreated, actionsSkipped, context.CorrelationId);
    }
}

public class ReminderCandidateQueryService
{
    private readonly FleetHubDbContext _db;

    public ReminderCandidateQueryService(FleetHubDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReminderCandidate>> GetReminderCandidatesAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(7);

        return await _db.WorkOrders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(wo => wo.Status == "Open" || wo.Status == "InProgress")
            .Where(wo => wo.RequestedDate >= now && wo.RequestedDate <= horizon)
            .Select(wo => new ReminderCandidate
            {
                WorkOrderId = wo.Id,
                WorkOrderNumber = wo.WorkOrderNumber,
                EquipmentName = wo.Equipment != null ? wo.Equipment.Name : "",
                ServiceType = wo.ServiceType,
                DaysUntilDue = (int)(wo.RequestedDate - now).TotalDays,
                AssignedToUserId = wo.AssignedToUserId,
                OrganizationId = wo.OrganizationId
            })
            .ToListAsync(ct);
    }
}

public class ReminderCandidate
{
    public Guid WorkOrderId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public int DaysUntilDue { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid OrganizationId { get; set; }
}
