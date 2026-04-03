using IronvaleFleetHub.Api.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.WorkOrders.Queries;

public record WorkOrderCalendarItem(
    Guid Id,
    string Title,
    DateTime Start,
    DateTime End,
    string Status,
    string Priority,
    string ServiceType
);

public record GetWorkOrderCalendarQuery(DateTime Start, DateTime End) : IRequest<List<WorkOrderCalendarItem>>, ISkipValidation;

public class GetWorkOrderCalendarQueryHandler : IRequestHandler<GetWorkOrderCalendarQuery, List<WorkOrderCalendarItem>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetWorkOrderCalendarQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<WorkOrderCalendarItem>> Handle(GetWorkOrderCalendarQuery request, CancellationToken ct)
    {
        return await _db.WorkOrders
            .Include(w => w.Equipment)
            .Where(w => w.OrganizationId == _tenant.OrganizationId
                && w.RequestedDate >= request.Start
                && w.RequestedDate <= request.End)
            .AsNoTracking()
            .Select(w => new WorkOrderCalendarItem(
                w.Id,
                w.WorkOrderNumber + " - " + (w.Equipment != null ? w.Equipment.Name : ""),
                w.RequestedDate,
                w.CompletedDate ?? w.RequestedDate.AddHours(4),
                w.Status,
                w.Priority,
                w.ServiceType
            ))
            .ToListAsync(ct);
    }
}