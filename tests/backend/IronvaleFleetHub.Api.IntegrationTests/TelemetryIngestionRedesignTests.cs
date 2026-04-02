using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.IntegrationTests.Infrastructure;
using IronvaleFleetHub.Api.Models;
using Xunit;

namespace IronvaleFleetHub.Api.IntegrationTests;

public sealed class TelemetryIngestionRedesignTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task IngestOnMainApi_Returns404()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateUnauthenticatedClient();

        client.DefaultRequestHeaders.Add("X-Api-Key", "some-key");

        var request = new
        {
            EquipmentId = TestSeedData.Org1EquipmentId,
            EventType = "periodic_reading",
            EngineHours = 4521.5,
            FuelLevel = 73.2,
            Temperature = 185.0,
            Latitude = 43.65,
            Longitude = -79.38
        };

        var response = await client.PostAsJsonAsync("/api/v1/telemetry/ingest", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_ReturnsStructuredMetricsResponse()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/telemetry/{TestSeedData.Org1EquipmentId}/metrics?range=7d");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        Assert.True(json.TryGetProperty("equipmentId", out var equipmentId));
        Assert.Equal(TestSeedData.Org1EquipmentId.ToString(), equipmentId.GetString());

        Assert.True(json.TryGetProperty("range", out var range));
        Assert.Equal("7d", range.GetString());

        Assert.True(json.TryGetProperty("metrics", out var metrics));
        Assert.True(metrics.TryGetProperty("engineHours", out var engineHours));
        Assert.True(engineHours.GetArrayLength() > 0);

        // Each metric point should have timestamp and value
        var firstPoint = engineHours[0];
        Assert.True(firstPoint.TryGetProperty("timestamp", out _));
        Assert.True(firstPoint.TryGetProperty("value", out _));
    }

    [Fact]
    public async Task GetMetrics_WithSpecificMetrics_ReturnsOnlyRequestedMetrics()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/telemetry/{TestSeedData.Org1EquipmentId}/metrics?range=7d&metrics=temperature");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        Assert.True(json.TryGetProperty("metrics", out var metrics));
        Assert.True(metrics.TryGetProperty("temperature", out _));
    }

    [Fact]
    public async Task GetAlerts_ReturnsPaginatedResponse()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/alerts?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        Assert.True(json.TryGetProperty("data", out var data));
        Assert.True(data.ValueKind == JsonValueKind.Array);

        Assert.True(json.TryGetProperty("pagination", out var pagination));
        Assert.True(pagination.TryGetProperty("page", out var page));
        Assert.Equal(1, page.GetInt32());
        Assert.True(pagination.TryGetProperty("pageSize", out var pageSize));
        Assert.Equal(10, pageSize.GetInt32());
        Assert.True(pagination.TryGetProperty("totalCount", out _));
    }

    [Fact]
    public async Task ThresholdManagement_CRUD()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient(role: "Admin");

        // Create
        var createRequest = new
        {
            MetricName = "Temperature",
            WarningValue = 90.0,
            CriticalValue = 110.0
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/alerts/thresholds", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(created.TryGetProperty("id", out var idProp));
        var thresholdId = idProp.GetString();
        Assert.NotNull(thresholdId);

        // Read
        var listResponse = await client.GetAsync("/api/v1/alerts/thresholds");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var thresholds = await listResponse.Content.ReadFromJsonAsync<JsonElement[]>(JsonOptions);
        Assert.NotNull(thresholds);
        Assert.Contains(thresholds, t => t.GetProperty("id").GetString() == thresholdId);

        // Update
        var updateRequest = new
        {
            MetricName = "Temperature",
            WarningValue = 95.0,
            CriticalValue = 115.0
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/alerts/thresholds/{thresholdId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(95.0, updated.GetProperty("warningValue").GetDouble());

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/v1/alerts/thresholds/{thresholdId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ThresholdManagement_RequiresAdminOrFleetManager()
    {
        await using var factory = new ApiWebApplicationFactory();

        // Test with Operator role (should be forbidden)
        using var operatorClient = factory.CreateAuthenticatedClient(
            objectId: "dev-op-001",
            role: "Operator");

        var createRequest = new
        {
            MetricName = "Temperature",
            WarningValue = 90.0,
            CriticalValue = 110.0
        };

        var response = await operatorClient.PostAsJsonAsync("/api/v1/alerts/thresholds", createRequest);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Test with FleetManager role (should work)
        using var fmClient = factory.CreateAuthenticatedClient(
            objectId: "dev-fm-001",
            role: "FleetManager");

        var fmResponse = await fmClient.PostAsJsonAsync("/api/v1/alerts/thresholds", createRequest);
        Assert.Equal(HttpStatusCode.Created, fmResponse.StatusCode);
    }

    [Fact]
    public async Task TelemetryEvent_HasOrganizationId()
    {
        await using var factory = new ApiWebApplicationFactory();

        // Verify seed data has OrganizationId populated
        var hasOrgId = await factory.ExecuteDbContextAsync(async db =>
        {
            var hasAny = await db.TelemetryEvents.IgnoreQueryFilters().AnyAsync();
            if (!hasAny) return false;
            var hasEmpty = await db.TelemetryEvents.IgnoreQueryFilters()
                .AnyAsync(e => e.OrganizationId == Guid.Empty);
            return !hasEmpty;
        });

        Assert.True(hasOrgId, "All TelemetryEvent records should have a non-empty OrganizationId.");
    }
}
