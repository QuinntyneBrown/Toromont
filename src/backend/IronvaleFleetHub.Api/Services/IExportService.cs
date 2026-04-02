using IronvaleFleetHub.Api.DTOs;

namespace IronvaleFleetHub.Api.Services;

public interface IExportService
{
    Task<byte[]> ExportToPdfAsync(ReportResponse report, CancellationToken ct = default);
    Task<byte[]> ExportToExcelAsync(ReportResponse report, CancellationToken ct = default);
    Task<byte[]> ExportToCsvAsync(ReportResponse report, CancellationToken ct = default);
}
