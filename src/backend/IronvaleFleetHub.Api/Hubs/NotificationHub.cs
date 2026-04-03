using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IHubAudienceResolver _audienceResolver;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(IHubAudienceResolver audienceResolver, ILogger<NotificationHub> logger)
    {
        _audienceResolver = audienceResolver;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var audience = await _audienceResolver.ResolveAsync(Context.User!, Context.ConnectionAborted);

        if (audience is null)
        {
            _logger.LogWarning("Connection {ConnectionId} rejected: unable to resolve audience", Context.ConnectionId);
            throw new HubException("Unable to resolve notification audience.");
        }

        // Canonical group names: user-{internalUserId} and org-{organizationId}
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{audience.UserId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"org-{audience.OrganizationId}");

        _logger.LogInformation(
            "Connection {ConnectionId} joined groups user-{UserId}, org-{OrgId}",
            Context.ConnectionId, audience.UserId, audience.OrganizationId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
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
