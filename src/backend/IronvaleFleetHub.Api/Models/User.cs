namespace IronvaleFleetHub.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string EntraObjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Organization? Organization { get; set; }
}
