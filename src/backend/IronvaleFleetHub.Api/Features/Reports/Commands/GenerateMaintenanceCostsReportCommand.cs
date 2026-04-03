using FluentValidation;
using MediatR;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Reports.Commands;

public record GenerateMaintenanceCostsReportCommand(ReportRequest Request) : IRequest<ReportResponse>;

public class GenerateMaintenanceCostsReportCommandHandler
    : IRequestHandler<GenerateMaintenanceCostsReportCommand, ReportResponse>
{
    private readonly IReportGenerationService _reportService;
    private readonly ITenantContext _tenant;

    public GenerateMaintenanceCostsReportCommandHandler(
        IReportGenerationService reportService, ITenantContext tenant)
    {
        _reportService = reportService;
        _tenant = tenant;
    }

    public async Task<ReportResponse> Handle(GenerateMaintenanceCostsReportCommand request, CancellationToken ct)
    {
        return await _reportService.GenerateMaintenanceCostsAsync(request.Request, _tenant.OrganizationId, ct);
    }
}

public class GenerateMaintenanceCostsReportCommandValidator : AbstractValidator<GenerateMaintenanceCostsReportCommand>
{
    public GenerateMaintenanceCostsReportCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.StartDate)
            .LessThan(x => x.Request.EndDate)
            .When(x => x.Request != null)
            .WithMessage("StartDate must be before EndDate.");
    }
}
