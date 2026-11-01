using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shared.Helpers;

namespace ChatService.Services;

/// <summary>
/// HTTP fallback for profile and connection facts. Every failure is converted to
/// a null/unknown result so a slow or unavailable UserService cannot take the
/// chat request path down; callers decide whether to fail open or closed.
/// </summary>
public sealed class UserDirectoryClient(
    HttpClient httpClient,
    ILogger<UserDirectoryClient> logger) : IUserDirectoryClient
{
    public const string UserIdHeaderName = "X-User-Id";

    public async Task<UserDirectoryProfile?> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        try
        {
            using var response = await httpClient.GetAsync(
                $"users/profile/{Uri.EscapeDataString(userId)}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "UserService profile lookup for {UserId} returned {StatusCode}.",
                    userId,
                    (int)response.StatusCode);
                return null;
            }

            var envelope = await response.Content
                .ReadFromJsonAsync<Response<ProfileDto>>(cancellationToken);

            if (envelope?.Succeeded != true || envelope.Data is null)
            {
                logger.LogWarning(
                    "UserService profile lookup for {UserId} returned an unsuccessful envelope.",
                    userId);
                return null;
            }

            var displayName = string.Join(
                ' ',
                new[] { envelope.Data.FirstName, envelope.Data.LastName }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = userId;
            }

            return new UserDirectoryProfile(
                userId,
                displayName,
                envelope.Data.Email,
                envelope.Data.ProfilePictureUrl);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException
                                       or TaskCanceledException
                                       or JsonException
                                       or NotSupportedException)
        {
            logger.LogWarning(
                ex,
                "UserService profile lookup for {UserId} failed; treating the profile as unavailable.",
                userId);
            return null;
        }
    }

    public async Task<ConnectionVerificationResult> VerifyConnectionAsync(
        string userId,
        string otherUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(otherUserId);

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"users/connections/{Uri.EscapeDataString(otherUserId)}/status");

            // UserService resolves the caller from the same gateway header the
            // gateway adds on external requests. This call bypasses the gateway,
            // so ChatService supplies the already-authenticated identity.
            request.Headers.TryAddWithoutValidation(UserIdHeaderName, userId);

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "UserService connection check {UserId}->{OtherUserId} returned {StatusCode}.",
                    userId,
                    otherUserId,
                    (int)response.StatusCode);
                return ConnectionVerificationResult.Unknown;
            }

            var envelope = await response.Content
                .ReadFromJsonAsync<Response<ConnectionStatusDto>>(cancellationToken);

            if (envelope?.Succeeded != true || envelope.Data is null)
            {
                logger.LogWarning(
                    "UserService connection check {UserId}->{OtherUserId} returned an unsuccessful envelope.",
                    userId,
                    otherUserId);
                return ConnectionVerificationResult.Unknown;
            }

            return envelope.Data.Connected
                ? ConnectionVerificationResult.Connected
                : ConnectionVerificationResult.NotConnected;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException
                                       or TaskCanceledException
                                       or JsonException
                                       or NotSupportedException)
        {
            logger.LogWarning(
                ex,
                "UserService connection check {UserId}->{OtherUserId} failed.",
                userId,
                otherUserId);
            return ConnectionVerificationResult.Unknown;
        }
    }

    internal sealed class ProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    internal sealed class ConnectionStatusDto
    {
        public bool Connected { get; set; }
    }
}
