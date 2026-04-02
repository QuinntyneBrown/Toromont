namespace IronvaleFleetHub.Api.DTOs;

public class ReportDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Category { get; set; }
    public DateTime? Date { get; set; }
}
