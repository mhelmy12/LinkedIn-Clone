using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ChatService.Infrastructure;

/// <summary>
/// Authenticates requests using the identity the API Gateway derived from the
/// validated Keycloak session. ChatService is not the authentication boundary:
/// the gateway authorizes the request and YARP injects <c>X-User-Id</c> (the
/// Keycloak <c>sub</c>). This is the same trust model every other service uses
/// for REST; the hub needs a scheme so SignalR can populate
/// <c>Context.UserIdentifier</c> and enforce <c>RequireAuthorization()</c>.
/// </summary>
public sealed class GatewayHeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "GatewayHeader";

    public const string UserIdHeaderName = "X-User-Id";

    /// <summary>
    /// Reads the single gateway-injected user id header. Missing, blank or
    /// ambiguous (repeated) values are rejected.
    /// </summary>
    internal static string? ReadUserId(HttpRequest request)
    {
        var values = request.Headers[UserIdHeaderName];

        if (values.Count != 1)
        {
            return null;
        }

        var userId = values[0]?.Trim();

        return string.IsNullOrEmpty(userId) ? null : userId;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = ReadUserId(Request);

        if (userId is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim("sub", userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
