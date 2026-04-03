namespace IronvaleFleetHub.Api.Models;

public class WorkflowDispatchRecord
{
    public Guid Id { get; set; }
    public string DispatchKey { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
}
