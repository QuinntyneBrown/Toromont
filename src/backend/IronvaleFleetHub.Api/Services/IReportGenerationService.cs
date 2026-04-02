using IronvaleFleetHub.Api.DTOs;

namespace IronvaleFleetHub.Api.Services;

public interface IReportGenerationService
{
    Task<ReportResponse> GenerateFleetUtilizationAsync(ReportRequest request, Guid organizationId, CancellationToken ct = default);
    Task<ReportResponse> GenerateMaintenanceCostsAsync(ReportRequest request, Guid organizationId, CancellationToken ct = default);
}
