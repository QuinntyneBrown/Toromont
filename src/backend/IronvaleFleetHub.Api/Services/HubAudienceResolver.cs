using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;

namespace IronvaleFleetHub.Api.Services;

public sealed class HubAudienceResolver : IHubAudienceResolver
{
    private readonly FleetHubDbContext _db;
    private readonly ILogger<HubAudienceResolver> _logger;

    public HubAudienceResolver(FleetHubDbContext db, ILogger<HubAudienceResolver> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<HubAudience?> ResolveAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        // Extract the Entra object ID from claims
        var objectId = principal.FindFirst("oid")?.Value
            ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(objectId))
        {
            _logger.LogWarning("Hub audience resolution failed: no object ID claim found");
            return null;
        }

        // Dev auth: check if organizationId claim is present directly
        var orgClaim = principal.FindFirst("organizationId")?.Value;

        // Resolve user from database (bypass tenant filter for bootstrap)
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.EntraObjectId == objectId && u.IsActive)
            .Select(u => new { u.Id, u.OrganizationId })
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            _logger.LogWarning("Hub audience resolution failed: user not found for objectId {ObjectId}", objectId);
            return null;
        }

        // Resolve active organization — use claim override or default membership
        Guid organizationId;
        if (!string.IsNullOrEmpty(orgClaim) && Guid.TryParse(orgClaim, out var claimOrgId))
        {
            organizationId = claimOrgId;
        }
        else
        {
            // Use user's default organization
            var membership = await _db.UserOrganizations
                .IgnoreQueryFilters()
                .Where(uo => uo.UserId == user.Id && uo.IsActive)
                .OrderByDescending(uo => uo.IsDefault)
                .Select(uo => uo.OrganizationId)
                .FirstOrDefaultAsync(ct);

            organizationId = membership != Guid.Empty ? membership : user.OrganizationId;
        }

        _logger.LogDebug(
            "Hub audience resolved: UserId={UserId}, OrgId={OrgId}",
            user.Id, organizationId);

        return new HubAudience(user.Id, organizationId);
    }
}
