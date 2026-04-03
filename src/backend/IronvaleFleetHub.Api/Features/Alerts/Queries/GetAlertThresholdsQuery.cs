using IronvaleFleetHub.Api.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Alerts.Queries;

public record GetAlertThresholdsQuery : IRequest<List<AlertThreshold>>, ISkipValidation;

public class GetAlertThresholdsQueryHandler : IRequestHandler<GetAlertThresholdsQuery, List<AlertThreshold>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetAlertThresholdsQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<AlertThreshold>> Handle(GetAlertThresholdsQuery request, CancellationToken ct)
    {
        return await _db.AlertThresholds
            .Where(t => t.OrganizationId == _tenant.OrganizationId)
            .OrderBy(t => t.MetricName)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}