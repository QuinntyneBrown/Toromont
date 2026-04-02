using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Common;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.Users.Commands;

public record InviteUserCommand(string Email, string Role) : IRequest<Result<UserInvitation>>;

public class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    private static readonly string[] ValidRoles = ["Admin", "FleetManager", "Technician", "Operator"];

    public InviteUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).NotEmpty().Must(r => ValidRoles.Contains(r))
            .WithMessage("Invalid role. Must be one of: Admin, FleetManager, Technician, Operator");
    }
}

public class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, Result<UserInvitation>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<InviteUserCommandHandler> _logger;

    public InviteUserCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        ILogger<InviteUserCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<Result<UserInvitation>> Handle(InviteUserCommand request, CancellationToken ct)
    {
        var existingUser = await _db.Users
            .AnyAsync(u => u.Email == request.Email && u.OrganizationId == _tenant.OrganizationId, ct);

        if (existingUser)
            return Result<UserInvitation>.Failure("A user with this email already exists in this organization.");

        var existingInvite = await _db.UserInvitations
            .AnyAsync(i => i.Email == request.Email
                && i.OrganizationId == _tenant.OrganizationId
                && !i.IsUsed, ct);

        if (existingInvite)
            return Result<UserInvitation>.Failure("A pending invitation already exists for this email.");

        var invitation = new UserInvitation
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Role = request.Role,
            OrganizationId = _tenant.OrganizationId,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray().Concat(Guid.NewGuid().ToByteArray()).ToArray()),
            IsUsed = false,
            InvitedByUserId = _tenant.UserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.UserInvitations.Add(invitation);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User invitation sent to {Email} by {UserId}", request.Email, _tenant.UserId);
        return Result<UserInvitation>.Success(invitation);
    }
}
