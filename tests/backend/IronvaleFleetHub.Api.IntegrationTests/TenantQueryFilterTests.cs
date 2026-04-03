using System.Net;
using System.Net.Http.Json;
using IronvaleFleetHub.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace IronvaleFleetHub.Api.IntegrationTests;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task Sequential_requests_with_different_orgs_return_isolated_equipment()
    {
        await using var factory = new ApiWebApplicationFactory();

        // Org1 admin sees only Org1 equipment
        using var org1Client = factory.CreateAuthenticatedClient(
            objectId: TestSeedData.AdminObjectId, role: "Admin");

        var org1Response = await org1Client.GetAsync("/api/v1/equipment");
        Assert.Equal(HttpStatusCode.OK, org1Response.StatusCode);
        var org1Body = await org1Response.Content.ReadAsStringAsync();

        // Org2 admin sees only Org2 equipment
        using var org2Client = factory.CreateAuthenticatedClient(
            objectId: TestSeedData.Org2AdminObjectId, role: "Admin");

        var org2Response = await org2Client.GetAsync("/api/v1/equipment");
        Assert.Equal(HttpStatusCode.OK, org2Response.StatusCode);
        var org2Body = await org2Response.Content.ReadAsStringAsync();

        // Responses must differ — each org sees only its own data
        Assert.NotEqual(org1Body, org2Body);

        // Org1 equipment ID must not appear in Org2 response
        Assert.DoesNotContain(TestSeedData.Org1EquipmentId.ToString(), org2Body);

        // Org2 equipment ID must not appear in Org1 response
        Assert.DoesNotContain(TestSeedData.Org2EquipmentId.ToString(), org1Body);
    }

    [Fact]
    public async Task Org1_admin_cannot_see_org2_work_orders()
    {
        await using var factory = new ApiWebApplicationFactory();

        using var org1Client = factory.CreateAuthenticatedClient(
            objectId: TestSeedData.AdminObjectId, role: "Admin");

        var response = await org1Client.GetAsync("/api/v1/work-orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        // Org2 equipment should not appear in Org1 work orders
        Assert.DoesNotContain(TestSeedData.Org2EquipmentId.ToString(), body);
    }
}
