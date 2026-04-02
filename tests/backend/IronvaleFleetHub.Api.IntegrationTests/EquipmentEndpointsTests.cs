using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.IntegrationTests.Infrastructure;
using IronvaleFleetHub.Api.Models;
using Xunit;

namespace IronvaleFleetHub.Api.IntegrationTests;

public sealed class EquipmentEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task GetAll_returns_only_equipment_for_the_authenticated_tenant()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/equipment?skip=0&take=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PaginatedResponse<Equipment>>(JsonOptions);

        Assert.NotNull(payload);
        Assert.Equal(6, payload.Pagination.TotalItems);
        Assert.Equal(6, payload.Items.Count);
        Assert.All(payload.Items, equipment => Assert.Equal(TestSeedData.Org1Id, equipment.OrganizationId));
    }

    [Fact]
    public async Task GetById_returns_not_found_for_equipment_from_another_tenant()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/equipment/{TestSeedData.Org2EquipmentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_persists_a_new_equipment_record()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient();

        var request = new
        {
            Name = "CAT 336 Excavator #99",
            Make = "CAT",
            Model = "336",
            Year = 2025,
            SerialNumber = "CAT336-2025-099",
            Category = "Excavator",
            Location = "Ottawa, ON",
            Latitude = 45.4215,
            Longitude = -75.6972,
            GpsDeviceId = "GPS-336-099",
            PurchaseDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            WarrantyExpiration = new DateTime(2028, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Notes = "Integration test create",
        };

        var response = await client.PostAsJsonAsync("/api/v1/equipment", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Equipment>(JsonOptions);

        Assert.NotNull(created);
        Assert.Equal(TestSeedData.Org1Id, created.OrganizationId);
        Assert.Equal(request.SerialNumber, created.SerialNumber);
        Assert.Equal(request.Location, created.Location);

        var persisted = await factory.ExecuteDbContextAsync(db =>
            db.Equipment.AsNoTracking().SingleOrDefaultAsync(e => e.Id == created.Id));

        Assert.NotNull(persisted);
        Assert.Equal(request.SerialNumber, persisted.SerialNumber);
        Assert.Equal(request.GpsDeviceId, persisted.GpsDeviceId);
    }

    [Fact]
    public async Task Create_returns_bad_request_for_a_duplicate_serial_number_in_the_same_tenant()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient();

        var request = new
        {
            Name = "Duplicate Serial Unit",
            Make = "CAT",
            Model = "320",
            Year = 2024,
            SerialNumber = "CAT320-2022-001",
            Category = "Excavator",
            Location = "Toronto, ON",
            Latitude = 43.65,
            Longitude = -79.38,
        };

        var response = await client.PostAsJsonAsync("/api/v1/equipment", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = document.RootElement.GetProperty("error").GetString();

        Assert.Equal("Equipment with this serial number already exists in this organization.", error);
    }
}
