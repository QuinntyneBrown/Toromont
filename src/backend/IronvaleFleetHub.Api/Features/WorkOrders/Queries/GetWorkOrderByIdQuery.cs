using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.WorkOrders.Queries;

public record GetWorkOrderByIdQuery(Guid Id) : IRequest<WorkOrder?>;

public class GetWorkOrderByIdQueryHandler : IRequestHandler<GetWorkOrderByIdQuery, WorkOrder?>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetWorkOrderByIdQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<WorkOrder?> Handle(GetWorkOrderByIdQuery request, CancellationToken ct)
    {
        return await _db.WorkOrders
            .Include(w => w.Equipment)
            .Include(w => w.AssignedTo)
            .Include(w => w.History.OrderByDescending(h => h.ChangedAt))
                .ThenInclude(h => h.ChangedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.Id && w.OrganizationId == _tenant.OrganizationId, ct);
    }
}
