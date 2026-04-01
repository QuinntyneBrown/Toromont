namespace ToromontFleetHub.Api.Models;

public class UserInvitation
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string Token { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public Guid InvitedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
