using System.Security.Claims;

namespace IronvaleFleetHub.Api.Services;

public sealed record HubAudience(Guid UserId, Guid OrganizationId);

public interface IHubAudienceResolver
{
    Task<HubAudience?> ResolveAsync(ClaimsPrincipal principal, CancellationToken ct = default);
}
