using FluentValidation;
using MediatR;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Alerts.Commands;

public record CreateAlertThresholdCommand(
    string MetricName,
    double WarningValue,
    double CriticalValue,
    Guid? EquipmentId = null
) : IRequest<Result<AlertThreshold>>;

public class CreateAlertThresholdCommandHandler : IRequestHandler<CreateAlertThresholdCommand, Result<AlertThreshold>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public CreateAlertThresholdCommandHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<AlertThreshold>> Handle(CreateAlertThresholdCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.MetricName))
            return Result<AlertThreshold>.Failure("MetricName is required.");

        var threshold = new AlertThreshold
        {
            Id = Guid.NewGuid(),
            OrganizationId = _tenant.OrganizationId,
            EquipmentId = request.EquipmentId,
            MetricName = request.MetricName,
            WarningValue = request.WarningValue,
            CriticalValue = request.CriticalValue,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.AlertThresholds.Add(threshold);
        await _db.SaveChangesAsync(ct);

        return Result<AlertThreshold>.Success(threshold);
    }
}

public class CreateAlertThresholdCommandValidator : AbstractValidator<CreateAlertThresholdCommand>
{
    public CreateAlertThresholdCommandValidator()
    {
        RuleFor(x => x.MetricName).NotEmpty();
        RuleFor(x => x.WarningValue).GreaterThan(0);
        RuleFor(x => x.CriticalValue).GreaterThan(0);
        RuleFor(x => x.CriticalValue)
            .GreaterThanOrEqualTo(x => x.WarningValue)
            .WithMessage("CriticalValue must be greater than or equal to WarningValue.");
    }
}
