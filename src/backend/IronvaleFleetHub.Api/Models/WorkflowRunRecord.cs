namespace IronvaleFleetHub.Api.Models;

public class WorkflowRunRecord
{
    public Guid Id { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = "Running";
    public int Attempt { get; set; } = 1;
    public string? Error { get; set; }
}
