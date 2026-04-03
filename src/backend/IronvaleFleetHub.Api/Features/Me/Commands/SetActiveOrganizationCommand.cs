using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;
using IronvaleFleetHub.Api.Common;

namespace IronvaleFleetHub.Api.Features.Me.Commands;

public record SetActiveOrganizationCommand(Guid OrganizationId) : IRequest<Result<CurrentUserContextResponse>>, ISkipValidation;

public class SetActiveOrganizationCommandHandler : IRequestHandler<SetActiveOrganizationCommand, Result<CurrentUserContextResponse>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public SetActiveOrganizationCommandHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<CurrentUserContextResponse>> Handle(SetActiveOrganizationCommand request, CancellationToken ct)
    {
        // Verify user has active membership in the requested organization
        var membership = await _db.UserOrganizations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(uo =>
                uo.UserId == _tenant.UserId
                && uo.OrganizationId == request.OrganizationId
                && uo.IsActive, ct);

        if (membership == null)
            return Result<CurrentUserContextResponse>.Failure("User does not have an active membership in the requested organization.");

        var org = await _db.Organizations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId && o.IsActive, ct);

        if (org == null)
            return Result<CurrentUserContextResponse>.Failure("Organization not found or inactive.");

        var user = await _db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _tenant.UserId, ct);

        if (user == null)
            return Result<CurrentUserContextResponse>.Failure("User not found.");

        // Get all memberships for the response
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
                    IsActive = uo.OrganizationId == request.OrganizationId
                })
            .ToListAsync(ct);

        var response = new CurrentUserContextResponse
        {
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = user.Role
            },
            ActiveOrganizationId = request.OrganizationId,
            Memberships = memberships
        };

        return Result<CurrentUserContextResponse>.Success(response);
    }
}
