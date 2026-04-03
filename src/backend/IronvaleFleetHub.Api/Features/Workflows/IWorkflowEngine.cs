namespace IronvaleFleetHub.Api.Features.Workflows;

public interface IWorkflowEngine
{
    Task TriggerAsync(string workflowName, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowStatusDto>> GetStatusAsync(CancellationToken ct = default);
}

public class WorkflowStatusDto
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public DateTime? LastRunUtc { get; set; }
    public string? LastRunStatus { get; set; }
    public int TotalRuns { get; set; }
}
