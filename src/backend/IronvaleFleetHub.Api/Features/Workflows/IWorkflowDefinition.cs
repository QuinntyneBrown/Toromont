namespace IronvaleFleetHub.Api.Features.Workflows;

public interface IWorkflowDefinition
{
    string Name { get; }
    TimeSpan PollInterval { get; }
    bool Enabled(DevWorkflowOptions options);
    Task ExecuteAsync(WorkflowExecutionContext context, CancellationToken ct);
}

public class WorkflowExecutionContext
{
    public Guid RunId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public IServiceProvider Services { get; set; } = null!;
    public DevWorkflowOptions Options { get; set; } = new();
}
