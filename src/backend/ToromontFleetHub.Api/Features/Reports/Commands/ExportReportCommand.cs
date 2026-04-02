using MediatR;
using ToromontFleetHub.Api.Common;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Reports.Commands;

public record ExportReportResult(byte[] FileBytes, string ContentType, string FileName);

public record ExportReportCommand(string ReportType, ReportRequest Request, string Format)
    : IRequest<Result<ExportReportResult>>;

public class ExportReportCommandHandler : IRequestHandler<ExportReportCommand, Result<ExportReportResult>>
{
    private readonly IReportGenerationService _reportService;
    private readonly IExportService _exportService;
    private readonly ITenantContext _tenant;
    private readonly ILogger<ExportReportCommandHandler> _logger;

    public ExportReportCommandHandler(
        IReportGenerationService reportService, IExportService exportService,
        ITenantContext tenant, ILogger<ExportReportCommandHandler> logger)
    {
        _reportService = reportService;
        _exportService = exportService;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<Result<ExportReportResult>> Handle(ExportReportCommand request, CancellationToken ct)
    {
        ReportResponse report;

        switch (request.ReportType.ToLowerInvariant())
        {
            case "fleet-utilization":
                report = await _reportService.GenerateFleetUtilizationAsync(request.Request, _tenant.OrganizationId, ct);
                break;
            case "maintenance-costs":
                report = await _reportService.GenerateMaintenanceCostsAsync(request.Request, _tenant.OrganizationId, ct);
                break;
            default:
                return Result<ExportReportResult>.Failure($"Unknown report type: {request.ReportType}");
        }

        byte[] fileBytes;
        string contentType;
        string fileName;

        switch (request.Format.ToLowerInvariant())
        {
            case "excel":
            case "xlsx":
                fileBytes = await _exportService.ExportToExcelAsync(report, ct);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"{request.ReportType}-report.xlsx";
                break;
            case "csv":
                fileBytes = await _exportService.ExportToCsvAsync(report, ct);
                contentType = "text/csv";
                fileName = $"{request.ReportType}-report.csv";
                break;
            case "pdf":
            default:
                fileBytes = await _exportService.ExportToPdfAsync(report, ct);
                contentType = "application/pdf";
                fileName = $"{request.ReportType}-report.pdf";
                break;
        }

        _logger.LogInformation("Report {Type} exported as {Format} by user {UserId}",
            request.ReportType, request.Format, _tenant.UserId);

        return Result<ExportReportResult>.Success(new ExportReportResult(fileBytes, contentType, fileName));
    }
}
