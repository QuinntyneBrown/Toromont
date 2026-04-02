using System.Security.Claims;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ToromontFleetHub.Api.Middleware;

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, FleetHubDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Try Entra ID object ID first
            var objectId = context.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                           ?? context.User.FindFirstValue("oid")
                           ?? string.Empty;

            if (!string.IsNullOrEmpty(objectId))
            {
                var user = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.EntraObjectId == objectId);

                if (user != null)
                {
                    tenantContext.Set(user.OrganizationId, user.Id, user.Role);
                }
            }

            // Dev auth: use organizationId claim directly
            if (tenantContext.OrganizationId == Guid.Empty)
            {
                var orgClaim = context.User.FindFirstValue("organizationId");
                var role = context.User.FindFirstValue(ClaimTypes.Role) ?? "Admin";
                if (Guid.TryParse(orgClaim, out var claimOrgId))
                {
                    // Find first user in this org, or use a synthetic ID
                    var devUser = await db.Users.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.OrganizationId == claimOrgId);
                    var devUserId = devUser?.Id ?? Guid.NewGuid();
                    tenantContext.Set(claimOrgId, devUserId, devUser?.Role ?? role);
                }
            }

            // Header-based tenant override (last resort)
            if (tenantContext.OrganizationId == Guid.Empty)
            {
                var devOrgId = context.Request.Headers["X-Organization-Id"].FirstOrDefault();
                var devUserId = context.Request.Headers["X-User-Id"].FirstOrDefault();
                var devRole = context.Request.Headers["X-User-Role"].FirstOrDefault();

                if (Guid.TryParse(devOrgId, out var orgId) && Guid.TryParse(devUserId, out var userId))
                {
                    tenantContext.Set(orgId, userId, devRole ?? "Admin");
                }
            }
        }

        await _next(context);
    }
}
