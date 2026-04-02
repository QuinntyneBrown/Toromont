namespace IronvaleFleetHub.Api.Models;

public class UserOrganization
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public DateTime JoinedAt { get; set; }
    public User? User { get; set; }
    public Organization? Organization { get; set; }
}
