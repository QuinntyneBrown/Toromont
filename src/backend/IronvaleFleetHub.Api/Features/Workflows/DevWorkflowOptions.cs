namespace IronvaleFleetHub.Api.Features.Workflows;

public sealed class DevWorkflowOptions
{
    public bool Enabled { get; set; }
    public bool EnableServiceReminders { get; set; } = true;
    public bool EnableWorkOrderEscalation { get; set; } = true;
    public bool EnablePartsOrderStatusSync { get; set; } = true;
    public int SchedulerPeriodSeconds { get; set; } = 60;
    public int MaxRetryCount { get; set; } = 3;
}
