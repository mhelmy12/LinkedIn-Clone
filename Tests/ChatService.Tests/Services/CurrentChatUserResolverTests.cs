using System.Security.Claims;
using ChatService.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ChatService.Tests.Services;

public class CurrentChatUserResolverTests
{
    private readonly CurrentChatUserResolver _resolver = new();

    [Fact]
    public void ResolveUserId_PrefersAuthenticatedSubjectOverHeader()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "token-user")], "Bearer"))
        };
        context.Request.Headers[CurrentChatUserResolver.UserIdHeaderName] = "header-user";

        Assert.Equal("token-user", _resolver.ResolveUserId(context));
    }

    [Fact]
    public void ResolveUserId_FallsBackToGatewayHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CurrentChatUserResolver.UserIdHeaderName] = "header-user";

        Assert.Equal("header-user", _resolver.ResolveUserId(context));
    }

    [Fact]
    public void ResolveUserId_IgnoresUnauthenticatedPrincipal()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "spoofed")]))
        };

        Assert.Null(_resolver.ResolveUserId(context));
    }

    [Fact]
    public void ResolveUserId_ReturnsNullWhenNoIdentityIsPresent()
    {
        Assert.Null(_resolver.ResolveUserId(new DefaultHttpContext()));
    }
}
