using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace IronvaleFleetHub.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var orgId = Context.User?.FindFirst("org_id")?.Value;
        var userId = Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(orgId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"org-{orgId}");
            _logger.LogInformation("Connection {ConnectionId} joined org group {OrgId}", Context.ConnectionId, orgId);
        }

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            _logger.LogInformation("Connection {ConnectionId} joined user group {UserId}", Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var orgId = Context.User?.FindFirst("org_id")?.Value;
        var userId = Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(orgId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"org-{orgId}");
        }

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        _logger.LogInformation("Connection {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendToUser(string userId, string method, object payload)
    {
        await Clients.Group($"user-{userId}").SendAsync(method, payload);
    }

    public async Task SendToOrganization(string orgId, string method, object payload)
    {
        await Clients.Group($"org-{orgId}").SendAsync(method, payload);
    }
}
