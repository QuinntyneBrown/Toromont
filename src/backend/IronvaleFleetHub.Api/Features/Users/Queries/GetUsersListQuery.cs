using IronvaleFleetHub.Api.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Users.Queries;

public record GetUsersListQuery : IRequest<List<User>>, ISkipValidation;

public class GetUsersListQueryHandler : IRequestHandler<GetUsersListQuery, List<User>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetUsersListQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<User>> Handle(GetUsersListQuery request, CancellationToken ct)
    {
        return await _db.Users
            .Where(u => u.OrganizationId == _tenant.OrganizationId)
            .OrderBy(u => u.DisplayName)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}