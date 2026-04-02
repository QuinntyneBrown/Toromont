using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Users.Commands;

public record ActivateUserCommand(Guid UserId) : IRequest<Result<User>>;

public class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, Result<User>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IUserBlacklist _blacklist;
    private readonly ILogger<ActivateUserCommandHandler> _logger;

    public ActivateUserCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        IUserBlacklist blacklist,
        ILogger<ActivateUserCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _blacklist = blacklist;
        _logger = logger;
    }

    public async Task<Result<User>> Handle(ActivateUserCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.OrganizationId == _tenant.OrganizationId, ct);

        if (user is null)
            return Result<User>.Failure("Not found.");

        user.IsActive = true;
        await _db.SaveChangesAsync(ct);

        _blacklist.Remove(user.Id);

        _logger.LogInformation("User {TargetUserId} activated by {UserId}", request.UserId, _tenant.UserId);
        return Result<User>.Success(user);
    }
}
