using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.IntegrationTests.Infrastructure;
using IronvaleFleetHub.Api.Models;
using Xunit;

namespace IronvaleFleetHub.Api.IntegrationTests;

public sealed class WorkOrdersAndNotificationsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Create_work_order_creates_an_assignment_notification_for_the_technician()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient();

        var request = new CreateWorkOrderRequest
        {
            EquipmentId = TestSeedData.Org1EquipmentId,
            ServiceType = "Preventive",
            Priority = "High",
            Description = "Integration test work order",
            RequestedDate = DateTime.UtcNow,
            AssignedToUserId = TestSeedData.TechnicianUserId,
        };

        var response = await client.PostAsJsonAsync("/api/v1/work-orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<WorkOrder>(JsonOptions);

        Assert.NotNull(created);
        Assert.Equal("Open", created.Status);
        Assert.Equal(TestSeedData.TechnicianUserId, created.AssignedToUserId);
        Assert.StartsWith("WO-", created.WorkOrderNumber, StringComparison.Ordinal);

        var notifications = await factory.ExecuteDbContextAsync(db =>
            db.Notifications
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(n => n.UserId == TestSeedData.TechnicianUserId && n.Type == "WorkOrderAssigned")
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync());

        var notification = Assert.Single(
            notifications,
            n => n.Message.Contains(created.WorkOrderNumber, StringComparison.Ordinal));

        Assert.Equal(TestSeedData.Org1Id, notification.OrganizationId);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task MarkAllAsRead_marks_the_current_users_notifications_and_drops_unread_count_to_zero()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient();

        var unreadBefore = await client.GetFromJsonAsync<UnreadCountResponse>(
            "/api/v1/notifications/unread-count",
            JsonOptions);

        Assert.NotNull(unreadBefore);
        Assert.Equal(1, unreadBefore.UnreadCount);

        var markAllResponse = await client.PutAsync("/api/v1/notifications/read-all", null);

        Assert.Equal(HttpStatusCode.NoContent, markAllResponse.StatusCode);

        var unreadAfter = await client.GetFromJsonAsync<UnreadCountResponse>(
            "/api/v1/notifications/unread-count",
            JsonOptions);

        Assert.NotNull(unreadAfter);
        Assert.Equal(0, unreadAfter.UnreadCount);

        var remainingUnread = await factory.ExecuteDbContextAsync(db =>
            db.Notifications.IgnoreQueryFilters().CountAsync(n => n.UserId == TestSeedData.AdminUserId && !n.IsRead));

        Assert.Equal(0, remainingUnread);
    }

    private sealed record UnreadCountResponse(int UnreadCount);
}
