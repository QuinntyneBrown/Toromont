namespace IronvaleFleetHub.Api.DTOs;

public class DashboardStats
{
    public int TotalEquipment { get; set; }
    public int ActiveEquipment { get; set; }
    public int ServiceRequired { get; set; }
    public int OverdueWorkOrders { get; set; }
    public double FleetUtilization { get; set; }
    public string? TotalEquipmentTrend { get; set; }
    public string? ActiveEquipmentTrend { get; set; }
    public string? ServiceRequiredTrend { get; set; }
    public string? OverdueWorkOrdersTrend { get; set; }
    public string? FleetUtilizationTrend { get; set; }
}
