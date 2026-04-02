using MediatR;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Reports.Commands;

public record GenerateFleetUtilizationReportCommand(ReportRequest Request) : IRequest<ReportResponse>;

public class GenerateFleetUtilizationReportCommandHandler
    : IRequestHandler<GenerateFleetUtilizationReportCommand, ReportResponse>
{
    private readonly IReportGenerationService _reportService;
    private readonly ITenantContext _tenant;

    public GenerateFleetUtilizationReportCommandHandler(
        IReportGenerationService reportService, ITenantContext tenant)
    {
        _reportService = reportService;
        _tenant = tenant;
    }

    public async Task<ReportResponse> Handle(GenerateFleetUtilizationReportCommand request, CancellationToken ct)
    {
        return await _reportService.GenerateFleetUtilizationAsync(request.Request, _tenant.OrganizationId, ct);
    }
}
