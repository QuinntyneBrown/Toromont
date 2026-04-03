using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Me.Queries;

public record GetCurrentUserContextQuery : IRequest<CurrentUserContextResponse?>, ISkipValidation;

public class GetCurrentUserContextQueryHandler : IRequestHandler<GetCurrentUserContextQuery, CurrentUserContextResponse?>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetCurrentUserContextQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<CurrentUserContextResponse?> Handle(GetCurrentUserContextQuery request, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _tenant.UserId, ct);

        if (user == null) return null;

        var memberships = await _db.UserOrganizations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(uo => uo.UserId == _tenant.UserId && uo.IsActive)
            .Join(_db.Organizations.IgnoreQueryFilters(),
                uo => uo.OrganizationId,
                o => o.Id,
                (uo, o) => new MembershipInfo
                {
                    OrganizationId = uo.OrganizationId,
                    OrganizationName = o.Name,
                    IsDefault = uo.IsDefault,
                    IsActive = uo.OrganizationId == _tenant.OrganizationId
                })
            .ToListAsync(ct);

        return new CurrentUserContextResponse
        {
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = user.Role
            },
            ActiveOrganizationId = _tenant.OrganizationId,
            Memberships = memberships
        };
    }
}
