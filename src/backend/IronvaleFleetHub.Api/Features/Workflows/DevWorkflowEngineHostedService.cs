using Microsoft.Extensions.Options;

namespace IronvaleFleetHub.Api.Features.Workflows;

public class DevWorkflowEngineHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly DevWorkflowOptions _options;
    private readonly ILogger<DevWorkflowEngineHostedService> _logger;
    private readonly Dictionary<string, DateTime> _lastSuccessfulRun = new();

    public DevWorkflowEngineHostedService(
        IServiceProvider services,
        IOptions<DevWorkflowOptions> options,
        ILogger<DevWorkflowEngineHostedService> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DevWorkflowEngine started (period: {Period}s)", _options.SchedulerPeriodSeconds);

        // Initial delay to let the application fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSchedulerTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in workflow scheduler loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.SchedulerPeriodSeconds), stoppingToken);
        }

        _logger.LogInformation("DevWorkflowEngine stopping");
    }

    private async Task RunSchedulerTickAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var definitions = scope.ServiceProvider.GetServices<IWorkflowDefinition>();
        var coordinator = scope.ServiceProvider.GetRequiredService<WorkflowExecutionCoordinator>();
        var clock = scope.ServiceProvider.GetRequiredService<IWorkflowClock>();

        foreach (var definition in definitions)
        {
            if (!definition.Enabled(_options))
                continue;

            if (_lastSuccessfulRun.TryGetValue(definition.Name, out var lastRun))
            {
                if (clock.UtcNow - lastRun < definition.PollInterval)
                    continue;
            }

            try
            {
                await coordinator.ExecuteWorkflowAsync(definition, scope.ServiceProvider, ct);
                _lastSuccessfulRun[definition.Name] = clock.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute workflow {WorkflowName}", definition.Name);
            }
        }
    }
}
