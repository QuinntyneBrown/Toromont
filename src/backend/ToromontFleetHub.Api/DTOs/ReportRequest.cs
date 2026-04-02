namespace ToromontFleetHub.Api.DTOs;

public class ReportRequest
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Format { get; set; } = "pdf";
    public List<Guid>? EquipmentIds { get; set; }
}
