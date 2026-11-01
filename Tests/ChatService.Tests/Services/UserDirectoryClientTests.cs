using System.Net;
using System.Text;
using ChatService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ChatService.Tests.Services;

public class UserDirectoryClientTests
{
    [Fact]
    public async Task GetProfileAsync_ParsesEnvelopeAndBuildsDisplayName()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """
            {
              "succeeded": true,
              "data": {
                "firstName": "Ada",
                "lastName": "Lovelace",
                "email": "ada@example.com",
                "profilePictureUrl": "https://example.com/ada.png"
              }
            }
            """));

        var client = CreateClient(handler);
        var profile = await client.GetProfileAsync("kc-ada");

        Assert.NotNull(profile);
        Assert.Equal("kc-ada", profile.UserId);
        Assert.Equal("Ada Lovelace", profile.DisplayName);
        Assert.Equal("ada@example.com", profile.Email);
        Assert.Equal("https://example.com/ada.png", profile.ProfilePictureUrl);
        Assert.Equal("http://user-service/users/profile/kc-ada", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNullWhenUserIsNotFound()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        Assert.Null(await client.GetProfileAsync("missing"));
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNullOnTransportFailure()
    {
        var client = CreateClient(new StubHandler(_ => throw new HttpRequestException("boom")));

        Assert.Null(await client.GetProfileAsync("kc-ada"));
    }

    [Fact]
    public async Task VerifyConnectionAsync_SendsCallerHeaderAndParsesResult()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """
            { "succeeded": true, "data": { "connected": true } }
            """));

        var client = CreateClient(handler);
        var result = await client.VerifyConnectionAsync("kc-ada", "kc-grace");

        Assert.Equal(ConnectionVerificationResult.Connected, result);
        Assert.Equal("http://user-service/users/connections/kc-grace/status", handler.LastRequestUri);
        Assert.Equal("kc-ada", handler.LastUserIdHeader);
    }

    [Fact]
    public async Task VerifyConnectionAsync_ReturnsNotConnectedForDefinitiveNegative()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """
            { "succeeded": true, "data": { "connected": false } }
            """));

        var result = await CreateClient(handler)
            .VerifyConnectionAsync("kc-ada", "kc-grace");

        Assert.Equal(ConnectionVerificationResult.NotConnected, result);
    }

    [Fact]
    public async Task VerifyConnectionAsync_ReturnsUnknownForNonSuccessStatus()
    {
        var client = CreateClient(new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        Assert.Equal(
            ConnectionVerificationResult.Unknown,
            await client.VerifyConnectionAsync("kc-ada", "kc-grace"));
    }

    [Fact]
    public async Task VerifyConnectionAsync_ReturnsUnknownOnTransportFailure()
    {
        var client = CreateClient(new StubHandler(_ => throw new HttpRequestException("offline")));

        Assert.Equal(
            ConnectionVerificationResult.Unknown,
            await client.VerifyConnectionAsync("kc-ada", "kc-grace"));
    }

    private static UserDirectoryClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://user-service/") },
            NullLogger<UserDirectoryClient>.Instance);

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

    private sealed class StubHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public string? LastRequestUri { get; private set; }

        public string? LastUserIdHeader { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString();
            LastUserIdHeader = request.Headers.TryGetValues(
                UserDirectoryClient.UserIdHeaderName, out var values)
                ? values.SingleOrDefault()
                : null;

            return Task.FromResult(respond(request));
        }
    }
}
