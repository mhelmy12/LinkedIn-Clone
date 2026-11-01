using Microsoft.AspNetCore.SignalR;

namespace ChatService.Infrastructure;

/// <summary>
/// Uses the authenticated <c>sub</c> claim (populated from the gateway's
/// <c>X-User-Id</c> header) as the SignalR user identifier so
/// <c>Clients.User(userId)</c> targets every connection a user has, across
/// replicas (the Redis backplane forwards to them).
/// </summary>
public sealed class ChatUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var principal = connection.User;

        return principal?.FindFirst("sub")?.Value
            ?? principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }
}
