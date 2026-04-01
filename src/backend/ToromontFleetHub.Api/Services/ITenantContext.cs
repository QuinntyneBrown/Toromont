namespace ToromontFleetHub.Api.Services;

public interface ITenantContext
{
    Guid OrganizationId { get; }
    Guid UserId { get; }
    string UserRole { get; }
    void Set(Guid organizationId, Guid userId, string role);
}
