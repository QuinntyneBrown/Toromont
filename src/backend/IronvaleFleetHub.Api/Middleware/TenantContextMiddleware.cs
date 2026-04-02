using System.Security.Claims;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace IronvaleFleetHub.Api.Middleware;

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        FleetHubDbContext db,
        IUserBlacklist blacklist,
        IHostEnvironment env)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Skip tenant resolution for [AllowAnonymous] endpoints (e.g., accept-invite)
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            await _next(context);
            return;
        }

        // Try Entra ID object ID first
        var objectId = context.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                       ?? context.User.FindFirstValue("oid")
                       ?? string.Empty;

        if (!string.IsNullOrEmpty(objectId))
        {
            var user = await db.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.EntraObjectId == objectId);

            if (user == null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "User not found." });
                return;
            }

            if (blacklist.IsBlacklisted(user.Id))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "User has been deactivated." });
                return;
            }

            if (!user.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "User account is inactive." });
                return;
            }

            // Resolve active organization via X-Active-Organization header or default membership
            var activeOrgHeader = context.Request.Headers["X-Active-Organization"].FirstOrDefault();
            Guid? resolvedOrgId = null;

            if (Guid.TryParse(activeOrgHeader, out var requestedOrgId))
            {
                // Verify user has an active membership in the requested org
                var membership = await db.Set<Models.UserOrganization>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(uo => uo.UserId == user.Id
                        && uo.OrganizationId == requestedOrgId
                        && uo.IsActive);

                if (membership != null)
                    resolvedOrgId = requestedOrgId;
            }

            if (resolvedOrgId == null)
            {
                // Fall back to default membership
                var defaultMembership = await db.Set<Models.UserOrganization>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(uo => uo.UserId == user.Id && uo.IsActive && uo.IsDefault);

                if (defaultMembership != null)
                    resolvedOrgId = defaultMembership.OrganizationId;
                else
                {
                    // Fall back to any active membership
                    var anyMembership = await db.Set<Models.UserOrganization>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(uo => uo.UserId == user.Id && uo.IsActive);

                    if (anyMembership != null)
                        resolvedOrgId = anyMembership.OrganizationId;
                }
            }

            if (resolvedOrgId == null)
            {
                // Fall back to legacy User.OrganizationId if no UserOrganization records exist
                if (user.OrganizationId != Guid.Empty)
                    resolvedOrgId = user.OrganizationId;
            }

            if (resolvedOrgId == null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "No active organization membership." });
                return;
            }

            // Verify the organization exists and is active
            var org = await db.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == resolvedOrgId.Value);

            if (org == null || !org.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Organization is inactive or not found." });
                return;
            }

            tenantContext.Set(resolvedOrgId.Value, user.Id, user.Role);
            await _next(context);
            return;
        }

        // Dev auth: use organizationId claim directly — only in Development
        if (env.IsDevelopment())
        {
            var orgClaim = context.User.FindFirstValue("organizationId");
            var role = context.User.FindFirstValue(ClaimTypes.Role) ?? "Admin";
            if (Guid.TryParse(orgClaim, out var claimOrgId))
            {
                var org = await db.Organizations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == claimOrgId);

                if (org == null || !org.IsActive)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "Organization is inactive or not found." });
                    return;
                }

                var devUser = await db.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.OrganizationId == claimOrgId);
                var devUserId = devUser?.Id ?? Guid.NewGuid();
                tenantContext.Set(claimOrgId, devUserId, devUser?.Role ?? role);
                await _next(context);
                return;
            }
        }

        // If we reach here, tenant resolution failed — return 403
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "Unable to resolve tenant context." });
    }
}
