using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Workflows;

public class PartsOrderStatusWorkflow : IWorkflowDefinition
{
    public string Name => "PartsOrderStatusWorkflow";
    public TimeSpan PollInterval => TimeSpan.FromMinutes(10);

    public bool Enabled(DevWorkflowOptions options) => options.EnablePartsOrderStatusSync;

    public async Task ExecuteAsync(WorkflowExecutionContext context, CancellationToken ct)
    {
        var db = context.Services.GetRequiredService<FleetHubDbContext>();
        var eventSource = context.Services.GetRequiredService<DevPartsOrderEventSource>();
        var notificationService = context.Services.GetRequiredService<INotificationDispatchService>();
        var logger = context.Services.GetRequiredService<ILogger<PartsOrderStatusWorkflow>>();

        var events = await eventSource.GetUnprocessedEventsAsync(ct);
        var actionsCreated = 0;
        var actionsSkipped = 0;

        foreach (var evt in events)
        {
            var dispatchKey = $"parts-order-status:{evt.ExternalEventId}";
            var exists = await db.WorkflowDispatchRecords
                .AnyAsync(d => d.DispatchKey == dispatchKey, ct);

            if (exists)
            {
                actionsSkipped++;
                continue;
            }

            var order = await db.PartsOrders
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Id == evt.PartsOrderId, ct);

            if (order == null)
            {
                logger.LogWarning("Parts order {OrderId} not found for event {EventId}",
                    evt.PartsOrderId, evt.ExternalEventId);
                continue;
            }

            // Update order status
            order.Status = evt.NewStatus;

            // Mark event as processed
            evt.Processed = true;

            // Record dispatch
            db.WorkflowDispatchRecords.Add(new WorkflowDispatchRecord
            {
                Id = Guid.NewGuid(),
                DispatchKey = dispatchKey,
                WorkflowName = Name,
                CreatedAtUtc = DateTime.UtcNow,
                EntityType = "PartsOrder",
                EntityId = order.Id
            });

            // Notify the ordering user
            await notificationService.DispatchAsync(
                order.UserId,
                "OrderConfirmation",
                $"Order Update: {order.OrderNumber}",
                $"Your parts order {order.OrderNumber} status has been updated to '{evt.NewStatus}'." +
                (evt.TrackingNumber != null ? $" Tracking: {evt.TrackingNumber}" : ""),
                "PartsOrder",
                order.Id,
                ct);

            actionsCreated++;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "PartsOrderStatusWorkflow: {Events} events, {Created} processed, {Skipped} skipped (correlation: {CorrelationId})",
            events.Count, actionsCreated, actionsSkipped, context.CorrelationId);
    }
}

public class DevPartsOrderEventSource
{
    private readonly FleetHubDbContext _db;

    public DevPartsOrderEventSource(FleetHubDbContext db) => _db = db;

    public async Task<IReadOnlyList<DevPartsOrderEvent>> GetUnprocessedEventsAsync(CancellationToken ct)
    {
        return await _db.DevPartsOrderEvents
            .Where(e => !e.Processed)
            .OrderBy(e => e.EventTimestamp)
            .ToListAsync(ct);
    }
}
