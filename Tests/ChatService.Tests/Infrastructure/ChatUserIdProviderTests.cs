using System.Security.Claims;
using ChatService.Infrastructure;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ChatService.Tests.Infrastructure;

public class ChatUserIdProviderTests
{
    private readonly ChatUserIdProvider _provider = new();

    [Fact]
    public void GetUserId_ReturnsKeycloakSubject()
    {
        var connection = CreateConnection(new Claim("sub", "kc-user-1"));

        Assert.Equal("kc-user-1", _provider.GetUserId(connection));
    }

    [Fact]
    public void GetUserId_ReturnsNullWhenPrincipalIsMissing()
    {
        var connection = CreateConnection();

        Assert.Null(_provider.GetUserId(connection));
    }

    private static HubConnectionContext CreateConnection(params Claim[] claims)
    {
        ClaimsPrincipal? user = claims.Length > 0
            ? new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"))
            : null;

        var connection = new DefaultConnectionContext("connection-1")
        {
            User = user
        };

        return new HubConnectionContext(
            connection,
            new HubConnectionContextOptions(),
            NullLoggerFactory.Instance);
    }
}
