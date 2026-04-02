using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Users.Commands;

public record ChangeUserRoleCommand(Guid UserId, string Role) : IRequest<Result<User>>;

public class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand, Result<User>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<ChangeUserRoleCommandHandler> _logger;

    public ChangeUserRoleCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        ILogger<ChangeUserRoleCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<Result<User>> Handle(ChangeUserRoleCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.OrganizationId == _tenant.OrganizationId, ct);

        if (user is null)
            return Result<User>.Failure("Not found.");

        if (user.Id == _tenant.UserId)
            return Result<User>.Failure("Cannot change your own role.");

        var validRoles = new[] { "Admin", "FleetManager", "Technician", "Operator" };
        if (!validRoles.Contains(request.Role))
            return Result<User>.Failure($"Invalid role. Valid roles: {string.Join(", ", validRoles)}");

        user.Role = request.Role;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {TargetUserId} role changed to {Role} by {UserId}", request.UserId, request.Role, _tenant.UserId);
        return Result<User>.Success(user);
    }
}
