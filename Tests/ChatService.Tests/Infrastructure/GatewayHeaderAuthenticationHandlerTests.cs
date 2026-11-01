using System.Text.Encodings.Web;
using ChatService.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ChatService.Tests.Infrastructure;

public class GatewayHeaderAuthenticationHandlerTests
{
    [Fact]
    public void ReadUserId_ReturnsTrimmedHeaderValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[GatewayHeaderAuthenticationHandler.UserIdHeaderName] = "  user-1  ";

        Assert.Equal("user-1", GatewayHeaderAuthenticationHandler.ReadUserId(context.Request));
    }

    [Fact]
    public void ReadUserId_ReturnsNullWhenHeaderIsMissing()
    {
        Assert.Null(
            GatewayHeaderAuthenticationHandler.ReadUserId(new DefaultHttpContext().Request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReadUserId_ReturnsNullWhenHeaderIsBlank(string value)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[GatewayHeaderAuthenticationHandler.UserIdHeaderName] = value;

        Assert.Null(GatewayHeaderAuthenticationHandler.ReadUserId(context.Request));
    }

    [Fact]
    public void ReadUserId_ReturnsNullWhenHeaderIsRepeated()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(GatewayHeaderAuthenticationHandler.UserIdHeaderName, "user-1");
        context.Request.Headers.Append(GatewayHeaderAuthenticationHandler.UserIdHeaderName, "user-2");

        Assert.Null(GatewayHeaderAuthenticationHandler.ReadUserId(context.Request));
    }

    [Fact]
    public async Task AuthenticateAsync_ProducesSubjectPrincipal()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[GatewayHeaderAuthenticationHandler.UserIdHeaderName] = "user-1";

        var handler = CreateHandler();
        await handler.InitializeAsync(
            new AuthenticationScheme(
                GatewayHeaderAuthenticationHandler.SchemeName,
                displayName: null,
                handlerType: typeof(GatewayHeaderAuthenticationHandler)),
            context);

        var result = await ((IAuthenticationHandler)handler).AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("user-1", result.Principal?.FindFirst("sub")?.Value);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsNoResultWithoutHeader()
    {
        var handler = CreateHandler();
        await handler.InitializeAsync(
            new AuthenticationScheme(
                GatewayHeaderAuthenticationHandler.SchemeName,
                displayName: null,
                handlerType: typeof(GatewayHeaderAuthenticationHandler)),
            new DefaultHttpContext());

        var result = await ((IAuthenticationHandler)handler).AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Principal);
    }

    private static GatewayHeaderAuthenticationHandler CreateHandler() =>
        new(
            new TestOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions()),
            NullLoggerFactory.Instance,
            UrlEncoder.Default);

    private sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
