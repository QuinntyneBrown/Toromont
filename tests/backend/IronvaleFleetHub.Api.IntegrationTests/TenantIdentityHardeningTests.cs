using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.IntegrationTests.Infrastructure;
using IronvaleFleetHub.Api.Models;
using Xunit;

namespace IronvaleFleetHub.Api.IntegrationTests;

public sealed class TenantIdentityHardeningTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };
    [Fact]
    public async Task Request_WithNoOrgClaim_Returns403()
    {
        // Arrange: authenticated user with unknown objectId (not in DB)
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient(objectId: "non-existent-object-id");

        // Act
        var response = await client.GetAsync("/api/v1/equipment?skip=0&take=20");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithInactiveOrg_Returns403()
    {
        // Arrange: deactivate org1
        await using var factory = new ApiWebApplicationFactory();

        await factory.ExecuteDbContextAsync(async db =>
        {
            var org = await db.Organizations.FirstAsync(o => o.Id == TestSeedData.Org1Id);
            org.IsActive = false;
            await db.SaveChangesAsync();
        });

        using var client = factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/v1/equipment?skip=0&take=20");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeactivatedUser_IsImmediatelyBlocked()
    {
        // Arrange: admin deactivates the technician
        await using var factory = new ApiWebApplicationFactory();
        using var adminClient = factory.CreateAuthenticatedClient();

        var deactivateResponse = await adminClient.PutAsync(
            $"/api/v1/users/{TestSeedData.TechnicianUserId}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        // Act: deactivated technician tries to access an endpoint
        using var userClient = factory.CreateAuthenticatedClient(
            objectId: TestSeedData.TechnicianObjectId, role: "Technician");
        var response = await userClient.GetAsync("/api/v1/equipment?skip=0&take=20");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReactivatedUser_IsUnblocked()
    {
        // Arrange: deactivate then reactivate
        await using var factory = new ApiWebApplicationFactory();
        using var adminClient = factory.CreateAuthenticatedClient();

        await adminClient.PutAsync(
            $"/api/v1/users/{TestSeedData.TechnicianUserId}/deactivate", null);
        await adminClient.PutAsync(
            $"/api/v1/users/{TestSeedData.TechnicianUserId}/activate", null);

        // Act
        using var userClient = factory.CreateAuthenticatedClient(
            objectId: TestSeedData.TechnicianObjectId, role: "Technician");
        var response = await userClient.GetAsync("/api/v1/equipment?skip=0&take=20");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HeaderBasedTenantOverride_IsIgnored()
    {
        // Arrange: send X-Organization-Id and X-User-Id headers (old override mechanism)
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient();

        client.DefaultRequestHeaders.Add("X-Organization-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-User-Id", Guid.NewGuid().ToString());

        // Act: request should use the JWT-based org, not the header value
        var response = await client.GetAsync("/api/v1/equipment?skip=0&take=20");

        // Assert: the request works with the real tenant, proving headers are ignored
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MultiOrgUser_CanSwitchActiveOrg()
    {
        // Arrange: add admin user to org2 as well
        await using var factory = new ApiWebApplicationFactory();

        await factory.ExecuteDbContextAsync(async db =>
        {
            db.Set<UserOrganization>().Add(new UserOrganization
            {
                Id = Guid.NewGuid(),
                UserId = TestSeedData.AdminUserId,
                OrganizationId = TestSeedData.Org2Id,
                IsActive = true,
                IsDefault = false,
                JoinedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        // Act: request with org2 active via X-Active-Organization header
        using var client = factory.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Add("X-Active-Organization", TestSeedData.Org2Id.ToString());

        var response = await client.GetAsync("/api/v1/equipment?skip=0&take=20");

        // Assert: succeeds and returns org2's equipment
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PaginatedResponse<Equipment>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Items);
        Assert.All(payload.Items, eq => Assert.Equal(TestSeedData.Org2Id, eq.OrganizationId));
    }

    [Fact]
    public async Task AcceptInvite_CreatesUserAndMembership()
    {
        // Arrange: create an invitation via the admin
        await using var factory = new ApiWebApplicationFactory();
        using var adminClient = factory.CreateAuthenticatedClient();

        var inviteResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/users/invite",
            new { email = "newuser@test.com", role = "Technician" });
        Assert.True(inviteResponse.IsSuccessStatusCode, await inviteResponse.Content.ReadAsStringAsync());

        var invitation = await inviteResponse.Content.ReadFromJsonAsync<UserInvitation>();
        Assert.NotNull(invitation);
        Assert.False(string.IsNullOrEmpty(invitation.Token));

        // Act: accept the invitation (with test auth claims for the new user)
        using var newUserClient = factory.CreateAuthenticatedClient(
            objectId: "new-user-entra-id", role: "Technician");
        newUserClient.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeaderName, "newuser@test.com");

        var acceptResponse = await newUserClient.PostAsJsonAsync(
            "/api/v1/users/accept-invite",
            new { token = invitation.Token });

        // Assert
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var createdUser = await acceptResponse.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(createdUser);
        Assert.Equal("newuser@test.com", createdUser.Email);
        Assert.Equal("Technician", createdUser.Role);
        Assert.Equal(TestSeedData.Org1Id, createdUser.OrganizationId);

        // Verify the UserOrganization membership was created
        var hasMembership = await factory.ExecuteDbContextAsync(db =>
            db.Set<UserOrganization>()
                .AnyAsync(uo => uo.UserId == createdUser.Id
                    && uo.OrganizationId == TestSeedData.Org1Id
                    && uo.IsActive));
        Assert.True(hasMembership);
    }

    [Fact]
    public async Task UserList_OnlyReturnsCurrentOrgUsers()
    {
        // Arrange: authenticate as org1 admin
        await using var factory = new ApiWebApplicationFactory();
        using var orgAClient = factory.CreateAuthenticatedClient();

        // Act
        var response = await orgAClient.GetAsync("/api/v1/users");
        var users = await response.Content.ReadFromJsonAsync<List<User>>();

        // Assert: only org1 users returned (4 users in org1 from seed data)
        Assert.NotNull(users);
        Assert.All(users, u => Assert.Equal(TestSeedData.Org1Id, u.OrganizationId));
        Assert.Equal(4, users.Count);
    }
}
