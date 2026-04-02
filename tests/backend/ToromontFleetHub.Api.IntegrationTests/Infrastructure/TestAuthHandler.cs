using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ToromontFleetHub.Api.IntegrationTests.Infrastructure;

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "IntegrationTestScheme";
    public const string ObjectIdHeaderName = "X-Test-ObjectId";
    public const string RoleHeaderName = "X-Test-Role";
    public const string EmailHeaderName = "X-Test-Email";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var objectId = Request.Headers[ObjectIdHeaderName].FirstOrDefault() ?? TestSeedData.AdminObjectId;
        var role = Request.Headers[RoleHeaderName].FirstOrDefault() ?? "Admin";
        var email = Request.Headers[EmailHeaderName].FirstOrDefault() ?? "integration@test.local";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, objectId),
            new Claim(ClaimTypes.Name, "Integration Test User"),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("oid", objectId),
            new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", objectId),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
