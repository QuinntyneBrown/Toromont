using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Features.Workflows;

public class WorkflowExecutionCoordinator : IWorkflowEngine
{
    private readonly IServiceProvider _services;
    private readonly DevWorkflowOptions _options;
    private readonly IWorkflowClock _clock;
    private readonly ILogger<WorkflowExecutionCoordinator> _logger;

    public WorkflowExecutionCoordinator(
        IServiceProvider services,
        IOptions<DevWorkflowOptions> options,
        IWorkflowClock clock,
        ILogger<WorkflowExecutionCoordinator> logger)
    {
        _services = services;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task TriggerAsync(string workflowName, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var definitions = scope.ServiceProvider.GetServices<IWorkflowDefinition>();
        var definition = definitions.FirstOrDefault(d =>
            d.Name.Equals(workflowName, StringComparison.OrdinalIgnoreCase));

        if (definition == null)
        {
            _logger.LogWarning("Workflow '{WorkflowName}' not found", workflowName);
            return;
        }

        await ExecuteWorkflowAsync(definition, scope.ServiceProvider, ct);
    }

    public async Task<IReadOnlyList<WorkflowStatusDto>> GetStatusAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FleetHubDbContext>();
        var definitions = scope.ServiceProvider.GetServices<IWorkflowDefinition>();

        var statuses = new List<WorkflowStatusDto>();
        foreach (var def in definitions)
        {
            var lastRun = await db.WorkflowRunRecords
                .AsNoTracking()
                .Where(r => r.WorkflowName == def.Name)
                .OrderByDescending(r => r.StartedAtUtc)
                .FirstOrDefaultAsync(ct);

            var totalRuns = await db.WorkflowRunRecords
                .CountAsync(r => r.WorkflowName == def.Name, ct);

            statuses.Add(new WorkflowStatusDto
            {
                Name = def.Name,
                Enabled = def.Enabled(_options),
                LastRunUtc = lastRun?.StartedAtUtc,
                LastRunStatus = lastRun?.Status,
                TotalRuns = totalRuns
            });
        }

        return statuses;
    }

    public async Task ExecuteWorkflowAsync(IWorkflowDefinition definition, IServiceProvider scopedServices, CancellationToken ct)
    {
        var db = scopedServices.GetRequiredService<FleetHubDbContext>();
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        var runId = Guid.NewGuid();

        var runRecord = new WorkflowRunRecord
        {
            Id = runId,
            WorkflowName = definition.Name,
            StartedAtUtc = _clock.UtcNow,
            Status = "Running",
            Attempt = 1
        };

        db.WorkflowRunRecords.Add(runRecord);
        await db.SaveChangesAsync(ct);

        var context = new WorkflowExecutionContext
        {
            RunId = runId,
            StartedAtUtc = _clock.UtcNow,
            CorrelationId = correlationId,
            Services = scopedServices,
            Options = _options
        };

        int attempt = 0;
        Exception? lastError = null;

        while (attempt < _options.MaxRetryCount)
        {
            attempt++;
            try
            {
                _logger.LogInformation(
                    "Executing workflow {WorkflowName} (attempt {Attempt}, correlation {CorrelationId})",
                    definition.Name, attempt, correlationId);

                await definition.ExecuteAsync(context, ct);

                runRecord.Status = "Completed";
                runRecord.CompletedAtUtc = _clock.UtcNow;
                runRecord.Attempt = attempt;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Workflow {WorkflowName} completed (correlation {CorrelationId}, elapsed {Elapsed}ms)",
                    definition.Name, correlationId,
                    (_clock.UtcNow - context.StartedAtUtc).TotalMilliseconds);

                return;
            }
            catch (Exception ex) when (attempt < _options.MaxRetryCount)
            {
                lastError = ex;
                _logger.LogWarning(ex,
                    "Workflow {WorkflowName} failed on attempt {Attempt}, retrying",
                    definition.Name, attempt);
            }
        }

        runRecord.Status = "Failed";
        runRecord.CompletedAtUtc = _clock.UtcNow;
        runRecord.Attempt = attempt;
        runRecord.Error = lastError?.Message;
        await db.SaveChangesAsync(ct);

        _logger.LogError(lastError,
            "Workflow {WorkflowName} failed after {MaxRetries} attempts",
            definition.Name, _options.MaxRetryCount);
    }
}
