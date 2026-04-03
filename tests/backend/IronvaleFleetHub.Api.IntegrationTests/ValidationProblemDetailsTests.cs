using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IronvaleFleetHub.Api.IntegrationTests.Infrastructure;

namespace IronvaleFleetHub.Api.IntegrationTests;

public class ValidationProblemDetailsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ValidationProblemDetailsTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Post_InvalidWorkOrder_Returns400ProblemDetails()
    {
        var client = _factory.CreateAuthenticatedClient();

        // Send empty payload — missing required fields
        var response = await client.PostAsJsonAsync("/api/v1/work-orders", new
        {
            EquipmentId = Guid.Empty,
            ServiceType = "",
            Priority = "",
            Description = "",
            RequestedDate = DateTime.MinValue
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.Equal("Validation Failed", body.GetProperty("title").GetString());
        Assert.True(body.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Post_InvalidEquipment_Returns400ProblemDetails()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/equipment", new
        {
            Name = "",
            Make = "",
            Model = "",
            Year = 0,
            SerialNumber = "",
            Category = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("errors", out var errors));
        Assert.True(errors.EnumerateObject().Any());
    }
}
