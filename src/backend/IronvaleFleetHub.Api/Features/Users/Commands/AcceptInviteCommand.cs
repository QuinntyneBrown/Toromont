using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Features.Users.Commands;

public record AcceptInviteCommand(string Token, string EntraObjectId, string Email, string DisplayName) : IRequest<Result<User>>;

public class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, Result<User>>
{
    private readonly FleetHubDbContext _db;
    private readonly ILogger<AcceptInviteCommandHandler> _logger;

    public AcceptInviteCommandHandler(FleetHubDbContext db, ILogger<AcceptInviteCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<User>> Handle(AcceptInviteCommand request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.Token))
            return Result<User>.Failure("Invitation token is required.");

        if (string.IsNullOrEmpty(request.EntraObjectId))
            return Result<User>.Failure("Authenticated identity is required to accept an invitation.");

        var invitation = await _db.UserInvitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Token == request.Token && !i.IsUsed, ct);

        if (invitation is null)
            return Result<User>.Failure("Invalid or already-used invitation token.");

        // Check if user already exists with this email in this org
        var existingUser = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == invitation.Email && u.OrganizationId == invitation.OrganizationId, ct);

        if (existingUser)
            return Result<User>.Failure("A user with this email already exists in this organization.");

        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            EntraObjectId = request.EntraObjectId,
            Email = invitation.Email,
            DisplayName = request.DisplayName,
            Role = invitation.Role,
            OrganizationId = invitation.OrganizationId,
            IsActive = true,
            CreatedAt = now,
        };

        var membership = new UserOrganization
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrganizationId = invitation.OrganizationId,
            IsActive = true,
            IsDefault = true,
            JoinedAt = now,
        };

        invitation.IsUsed = true;

        _db.Users.Add(user);
        _db.Set<UserOrganization>().Add(membership);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} created via invitation {InvitationId}", user.Id, invitation.Id);
        return Result<User>.Success(user);
    }
}

public class AcceptInviteCommandValidator : AbstractValidator<AcceptInviteCommand>
{
    public AcceptInviteCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.EntraObjectId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.DisplayName).NotEmpty();
    }
}