using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Equipment.Queries;

public record GetEquipmentByIdQuery(Guid Id) : IRequest<Models.Equipment?>;

public class GetEquipmentByIdQueryHandler : IRequestHandler<GetEquipmentByIdQuery, Models.Equipment?>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetEquipmentByIdQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Models.Equipment?> Handle(GetEquipmentByIdQuery request, CancellationToken ct)
    {
        return await _db.Equipment
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.OrganizationId == _tenant.OrganizationId, ct);
    }
}
