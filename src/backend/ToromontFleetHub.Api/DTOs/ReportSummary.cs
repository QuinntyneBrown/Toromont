namespace ToromontFleetHub.Api.DTOs;

public class ReportSummary
{
    public int TotalEquipment { get; set; }
    public int ActiveWorkOrders { get; set; }
    public int CompletedWorkOrders { get; set; }
    public double AverageCompletionDays { get; set; }
    public decimal TotalPartsCost { get; set; }
}
