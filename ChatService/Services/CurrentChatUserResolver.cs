using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ChatService.Services;

/// <summary>
/// Resolves the caller's identity for chat endpoints and hub connections.
/// The gateway-injected <c>X-User-Id</c> (the Keycloak <c>sub</c>) is the
/// repository-wide identity source; an authenticated <c>sub</c> claim produced
/// by the gateway-header scheme takes precedence. No id translation is
/// performed: user identity is the Keycloak subject everywhere (2026-11-01
/// decision).
/// </summary>
public sealed class CurrentChatUserResolver
{
    public const string UserIdHeaderName = "X-User-Id";

    private const string SubjectClaim = "sub";

    /// <summary>
    /// Returns the caller's user id, or <see langword="null"/> when the request
    /// carries neither a JWT subject nor the gateway header. Endpoints must
    /// translate <see langword="null"/> into a <c>401</c> response.
    /// </summary>
    public string? ResolveUserId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var subject = GetSubject(context.User);
        if (!string.IsNullOrWhiteSpace(subject))
        {
            return subject;
        }

        // Trusted only because the API Gateway is the production ingress and
        // rewrites this header from the validated token (research R2).
        var header = context.Request.Headers[UserIdHeaderName].ToString();

        return string.IsNullOrWhiteSpace(header) ? null : header;
    }

    /// <summary>Returns the authenticated Keycloak subject for a principal.</summary>
    public string? GetSubject(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // MapInboundClaims is disabled, so the Keycloak claim keeps its name.
        // NameIdentifier is retained as a fallback for non-Keycloak dev tokens.
        return principal.FindFirst(SubjectClaim)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
