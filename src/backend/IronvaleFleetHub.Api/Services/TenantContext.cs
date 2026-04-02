namespace IronvaleFleetHub.Api.Services;

public class TenantContext : ITenantContext
{
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public string UserRole { get; private set; } = string.Empty;

    public void Set(Guid organizationId, Guid userId, string role)
    {
        OrganizationId = organizationId;
        UserId = userId;
        UserRole = role;
    }
}
