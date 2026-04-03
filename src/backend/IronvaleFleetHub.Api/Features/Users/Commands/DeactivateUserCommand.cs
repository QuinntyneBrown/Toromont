using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Users.Commands;

public record DeactivateUserCommand(Guid UserId) : IRequest<Result<User>>, ISkipValidation;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result<User>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IUserBlacklist _blacklist;
    private readonly ILogger<DeactivateUserCommandHandler> _logger;

    public DeactivateUserCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        IUserBlacklist blacklist,
        ILogger<DeactivateUserCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _blacklist = blacklist;
        _logger = logger;
    }

    public async Task<Result<User>> Handle(DeactivateUserCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.OrganizationId == _tenant.OrganizationId, ct);

        if (user is null)
            return Result<User>.Failure("Not found.");

        if (user.Id == _tenant.UserId)
            return Result<User>.Failure("Cannot deactivate your own account.");

        user.IsActive = false;
        await _db.SaveChangesAsync(ct);

        _blacklist.Add(user.Id);

        _logger.LogInformation("User {TargetUserId} deactivated by {UserId}", request.UserId, _tenant.UserId);
        return Result<User>.Success(user);
    }
}
