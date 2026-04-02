using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.IntegrationTests.Infrastructure;

internal sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"IronvaleIntegrationTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<FleetHubDbContext>>();
            services.RemoveAll<FleetHubDbContext>();

            services.AddDbContext<FleetHubDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient(string objectId = TestSeedData.AdminObjectId, string role = "Admin")
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        client.DefaultRequestHeaders.Add(TestAuthHandler.ObjectIdHeaderName, objectId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, role);

        return client;
    }

    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        client.DefaultRequestHeaders.Add(TestAuthHandler.SkipAuthHeaderName, "true");

        return client;
    }

    public async Task<T> ExecuteDbContextAsync<T>(Func<FleetHubDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FleetHubDbContext>();
        return await action(db);
    }

    public async Task ExecuteDbContextAsync(Func<FleetHubDbContext, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FleetHubDbContext>();
        await action(db);
    }

    public IUserBlacklist GetBlacklist()
    {
        return Services.GetRequiredService<IUserBlacklist>();
    }
}
