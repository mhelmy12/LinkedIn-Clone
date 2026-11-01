namespace ChatService.Services;

/// <summary>
/// The profile data ChatService needs for conversation headers and offline
/// notifications. It is a read model of UserService, never a database read
/// (Constitution I / FR-016).
/// </summary>
public sealed record UserDirectoryProfile(
    string UserId,
    string DisplayName,
    string? Email,
    string? ProfilePictureUrl);

/// <summary>
/// Result of a one-shot connection check against UserService. <see cref="Unknown"/>
/// means the service could not give a definitive answer (timeout, transport
/// error, unexpected payload) and the caller must fail closed without caching.
/// </summary>
public enum ConnectionVerificationResult
{
    Connected,
    NotConnected,
    Unknown
}

/// <summary>
/// Typed client for the UserService HTTP API used as the fallback when the
/// Kafka-fed projections are missing or stale (research R5/R6). Calls go
/// through Aspire service discovery, not the API Gateway.
/// </summary>
public interface IUserDirectoryClient
{
    /// <summary>
    /// Returns the user's profile, or <see langword="null"/> when the user does
    /// not exist or UserService is unavailable.
    /// </summary>
    Task<UserDirectoryProfile?> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the one-shot connection verification for a pair. The caller is
    /// carried in the gateway header so UserService can evaluate the pair
    /// without exposing an unauthenticated lookup.
    /// </summary>
    Task<ConnectionVerificationResult> VerifyConnectionAsync(
        string userId,
        string otherUserId,
        CancellationToken cancellationToken = default);
}
