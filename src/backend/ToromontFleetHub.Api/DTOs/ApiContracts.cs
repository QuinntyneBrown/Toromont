namespace ToromontFleetHub.Api.DTOs;

// --- Pagination ---

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

// --- Equipment ---

public class CreateEquipmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? GpsDeviceId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiration { get; set; }
    public string? Notes { get; set; }
}

public class UpdateEquipmentRequest
{
    public string? Name { get; set; }
    public string? Status { get; set; }
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? GpsDeviceId { get; set; }
    public string? Notes { get; set; }
}

// --- Work Orders ---

public class CreateWorkOrderRequest
{
    public Guid EquipmentId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Description { get; set; } = string.Empty;
    public DateTime RequestedDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
}

public class StatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

// --- Cart / Orders ---

public class AddCartItemRequest
{
    public Guid PartId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}

// --- Users ---

public class InviteUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

// --- Reports ---

public class ReportRequest
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Format { get; set; } = "pdf"; // pdf, xlsx, csv
    public List<Guid>? EquipmentIds { get; set; }
}

public class ReportResponse
{
    public string ReportTitle { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public ReportSummary Summary { get; set; } = new();
    public List<ReportDataPoint> DataPoints { get; set; } = new();
}

public class ReportDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Category { get; set; }
    public DateTime? Date { get; set; }
}

public class ReportSummary
{
    public int TotalEquipment { get; set; }
    public int ActiveWorkOrders { get; set; }
    public int CompletedWorkOrders { get; set; }
    public double AverageCompletionDays { get; set; }
    public decimal TotalPartsCost { get; set; }
}

// --- Dashboard ---

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

// --- Telemetry ---

public class TelemetryIngestionRequest
{
    public Guid EquipmentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public double EngineHours { get; set; }
    public double FuelLevel { get; set; }
    public double Temperature { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Payload { get; set; }
}
