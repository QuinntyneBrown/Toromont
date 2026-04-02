namespace IronvaleFleetHub.Api.DTOs;

public class StatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
