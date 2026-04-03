namespace IronvaleFleetHub.Api.Features.Workflows;

public interface IWorkflowClock
{
    DateTime UtcNow { get; }
}

public class SystemWorkflowClock : IWorkflowClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
