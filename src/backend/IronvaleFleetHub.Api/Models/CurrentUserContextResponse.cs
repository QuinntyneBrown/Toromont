namespace IronvaleFleetHub.Api.Models;

public class CurrentUserContextResponse
{
    public UserInfo User { get; set; } = new();
    public Guid ActiveOrganizationId { get; set; }
    public List<MembershipInfo> Memberships { get; set; } = new();
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class MembershipInfo
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}
