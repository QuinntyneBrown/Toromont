using Microsoft.AspNetCore.SignalR;

namespace ToromontFleetHub.Api.Hubs;

public class NotificationHub : Hub
{
    public async Task JoinOrganizationGroup(string organizationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"org-{organizationId}");
    }

    public async Task LeaveOrganizationGroup(string organizationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"org-{organizationId}");
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        await base.OnConnectedAsync();
    }
}
