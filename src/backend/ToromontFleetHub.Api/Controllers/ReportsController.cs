using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportGenerationService _reportService;
    private readonly IExportService _exportService;
    private readonly ITenantContext _tenant;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportGenerationService reportService,
        IExportService exportService,
        ITenantContext tenant,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _exportService = exportService;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpPost("fleet-utilization")]
    public async Task<ActionResult<ReportResponse>> FleetUtilization(
        [FromBody] ReportRequest request,
        CancellationToken ct)
    {
        var report = await _reportService.GenerateFleetUtilizationAsync(request, _tenant.OrganizationId, ct);
        return Ok(report);
    }

    [HttpPost("maintenance-costs")]
    public async Task<ActionResult<ReportResponse>> MaintenanceCosts(
        [FromBody] ReportRequest request,
        CancellationToken ct)
    {
        var report = await _reportService.GenerateMaintenanceCostsAsync(request, _tenant.OrganizationId, ct);
        return Ok(report);
    }

    [HttpPost("{type}/export")]
    public async Task<IActionResult> Export(
        string type,
        [FromBody] ReportRequest request,
        [FromQuery] string format = "pdf",
        CancellationToken ct = default)
    {
        ReportResponse report;

        switch (type.ToLowerInvariant())
        {
            case "fleet-utilization":
                report = await _reportService.GenerateFleetUtilizationAsync(request, _tenant.OrganizationId, ct);
                break;
            case "maintenance-costs":
                report = await _reportService.GenerateMaintenanceCostsAsync(request, _tenant.OrganizationId, ct);
                break;
            default:
                return BadRequest(new { Error = $"Unknown report type: {type}" });
        }

        byte[] fileBytes;
        string contentType;
        string fileName;

        switch (format.ToLowerInvariant())
        {
            case "excel":
            case "xlsx":
                fileBytes = await _exportService.ExportToExcelAsync(report, ct);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"{type}-report.xlsx";
                break;
            case "csv":
                fileBytes = await _exportService.ExportToCsvAsync(report, ct);
                contentType = "text/csv";
                fileName = $"{type}-report.csv";
                break;
            case "pdf":
            default:
                fileBytes = await _exportService.ExportToPdfAsync(report, ct);
                contentType = "application/pdf";
                fileName = $"{type}-report.pdf";
                break;
        }

        _logger.LogInformation("Report {Type} exported as {Format} by user {UserId}", type, format, _tenant.UserId);
        return File(fileBytes, contentType, fileName);
    }
}
