using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "RequireAdmin")]
public class UsersController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<UsersController> _logger;

    public UsersController(FleetHubDbContext db, ITenantContext tenant, ILogger<UsersController> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAll(CancellationToken ct)
    {
        var users = await _db.Users
            .Where(u => u.OrganizationId == _tenant.OrganizationId)
            .OrderBy(u => u.DisplayName)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(users);
    }

    [HttpPost("invite")]
    public async Task<ActionResult<UserInvitation>> InviteUser(
        [FromBody] InviteUserRequest request,
        CancellationToken ct)
    {
        var existingUser = await _db.Users
            .AnyAsync(u => u.Email == request.Email && u.OrganizationId == _tenant.OrganizationId, ct);

        if (existingUser)
            return BadRequest(new { Error = "A user with this email already exists in this organization." });

        var existingInvite = await _db.UserInvitations
            .AnyAsync(i => i.Email == request.Email
                && i.OrganizationId == _tenant.OrganizationId
                && !i.IsUsed, ct);

        if (existingInvite)
            return BadRequest(new { Error = "A pending invitation already exists for this email." });

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
        return CreatedAtAction(nameof(GetAll), invitation);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<User>> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.OrganizationId == _tenant.OrganizationId, ct);

        if (user is null)
            return NotFound();

        if (user.Id == _tenant.UserId)
            return BadRequest(new { Error = "Cannot change your own role." });

        var validRoles = new[] { "Admin", "FleetManager", "Technician", "Viewer" };
        if (!validRoles.Contains(request.Role))
            return BadRequest(new { Error = $"Invalid role. Valid roles: {string.Join(", ", validRoles)}" });

        user.Role = request.Role;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {TargetUserId} role changed to {Role} by {UserId}", id, request.Role, _tenant.UserId);
        return Ok(user);
    }

    [HttpPut("{id:guid}/deactivate")]
    public async Task<ActionResult<User>> Deactivate(Guid id, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.OrganizationId == _tenant.OrganizationId, ct);

        if (user is null)
            return NotFound();

        if (user.Id == _tenant.UserId)
            return BadRequest(new { Error = "Cannot deactivate your own account." });

        user.IsActive = false;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {TargetUserId} deactivated by {UserId}", id, _tenant.UserId);
        return Ok(user);
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<ActionResult<User>> Activate(Guid id, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.OrganizationId == _tenant.OrganizationId, ct);

        if (user is null)
            return NotFound();

        user.IsActive = true;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {TargetUserId} activated by {UserId}", id, _tenant.UserId);
        return Ok(user);
    }
}
