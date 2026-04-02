namespace IronvaleFleetHub.Api.DTOs;

public class ReportResponse
{
    public string ReportTitle { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public ReportSummary Summary { get; set; } = new();
    public List<ReportDataPoint> DataPoints { get; set; } = new();
}
