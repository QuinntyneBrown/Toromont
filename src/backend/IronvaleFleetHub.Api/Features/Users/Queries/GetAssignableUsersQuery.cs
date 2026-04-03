using IronvaleFleetHub.Api.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Users.Queries;

public record GetAssignableUsersQuery : IRequest<List<User>>, ISkipValidation;

public class GetAssignableUsersQueryHandler : IRequestHandler<GetAssignableUsersQuery, List<User>>
{
    private static readonly string[] AssignableRoles = { "Technician", "FleetManager" };

    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetAssignableUsersQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<User>> Handle(GetAssignableUsersQuery request, CancellationToken ct)
    {
        return await _db.Users
            .Where(u => u.OrganizationId == _tenant.OrganizationId
                        && u.IsActive
                        && AssignableRoles.Contains(u.Role))
            .OrderBy(u => u.DisplayName)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}