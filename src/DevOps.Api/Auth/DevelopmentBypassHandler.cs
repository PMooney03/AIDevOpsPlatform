using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DevOps.Api.Auth;

public sealed class DevelopmentBypassHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevelopmentBypass";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(SchemeName);
        identity.AddClaim(new Claim(ClaimTypes.Name, "integration-test"));
        identity.AddClaim(new Claim(ClaimTypes.Role, PlatformRoles.Administrator));
        identity.AddClaim(new Claim(ClaimTypes.Role, PlatformRoles.Operator));
        identity.AddClaim(new Claim(ClaimTypes.Role, PlatformRoles.Viewer));
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
