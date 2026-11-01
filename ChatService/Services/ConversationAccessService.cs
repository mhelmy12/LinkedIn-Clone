using ChatService.Data;
using ChatService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ChatService.Services;

public enum ConversationAccessStatus
{
    Allowed,
    NotParticipant,
    NotConnected
}

public sealed record ConversationAccessResult(ConversationAccessStatus Status)
{
    public bool IsAllowed => Status == ConversationAccessStatus.Allowed;
}

/// <summary>
/// Single enforcement point for FR-001/FR-002: the caller must be a participant
/// of the conversation and the two participants must still have an accepted
/// connection. Connection facts come from the Kafka-fed projection; a miss or a
/// negative row triggers one bounded UserService verification (research R5)
/// whose result is cached for a short window.
/// </summary>
public sealed class ConversationAccessService(
    ChatDbContext db,
    IUserDirectoryClient userDirectory,
    IMemoryCache cache,
    ILogger<ConversationAccessService> logger)
{
    /// <summary>A positive verification is stable; cache it long enough to absorb bursts.</summary>
    private static readonly TimeSpan PositiveCacheDuration = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Negative verification is cached briefly so a just-accepted connection
    /// becomes usable quickly once UserService reports it, while a flood of
    /// non-connection attempts cannot hammer the fallback API.
    /// </summary>
    private static readonly TimeSpan NegativeCacheDuration = TimeSpan.FromSeconds(10);

    public static bool IsParticipant(Conversation conversation, string requesterId)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        if (string.IsNullOrWhiteSpace(requesterId))
        {
            return false;
        }

        return string.Equals(conversation.Participant1Id, requesterId, StringComparison.Ordinal)
            || string.Equals(conversation.Participant2Id, requesterId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Authorizes an operation on an existing conversation: participant check
    /// first, then the active-connection check against the peer.
    /// </summary>
    public async Task<ConversationAccessResult> AuthorizeConversationAsync(
        Conversation conversation,
        string requesterId,
        CancellationToken cancellationToken)
    {
        if (!IsParticipant(conversation, requesterId))
        {
            return new ConversationAccessResult(ConversationAccessStatus.NotParticipant);
        }

        var peerId = string.Equals(
            conversation.Participant1Id, requesterId, StringComparison.Ordinal)
            ? conversation.Participant2Id
            : conversation.Participant1Id;

        return await AuthorizePairAsync(requesterId, peerId, cancellationToken);
    }

    /// <summary>
    /// Authorizes creating/using the conversation for a pair before a
    /// conversation row exists.
    /// </summary>
    public async Task<ConversationAccessResult> AuthorizePairAsync(
        string requesterId,
        string otherUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requesterId))
        {
            return new ConversationAccessResult(ConversationAccessStatus.NotParticipant);
        }

        if (string.IsNullOrWhiteSpace(otherUserId)
            || string.Equals(requesterId, otherUserId, StringComparison.Ordinal))
        {
            return new ConversationAccessResult(ConversationAccessStatus.NotConnected);
        }

        return await AreConnectedAsync(requesterId, otherUserId, cancellationToken)
            ? new ConversationAccessResult(ConversationAccessStatus.Allowed)
            : new ConversationAccessResult(ConversationAccessStatus.NotConnected);
    }

    /// <summary>
    /// Returns whether the pair currently has an accepted connection. The
    /// projection is authoritative when it says active; otherwise a single
    /// UserService verification fills the eventual-consistency gap.
    /// </summary>
    public async Task<bool> AreConnectedAsync(
        string userId,
        string otherUserId,
        CancellationToken cancellationToken)
    {
        var (lower, higher) = PairNormalizer.Normalize(userId, otherUserId);

        var projection = await db.ChatConnectionProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.UserId == lower && p.OtherUserId == higher,
                cancellationToken);

        if (projection is { IsActive: true })
        {
            return true;
        }

        var cacheKey = $"chat:connection:{lower}:{higher}";

        if (cache.TryGetValue(cacheKey, out bool cached))
        {
            // A cached positive can predate a removal event. When the projection
            // now carries an inactive row, re-verify instead of trusting it;
            // a cached negative stays authoritative to avoid hammering the API.
            if (!cached || projection is null)
            {
                return cached;
            }
        }

        var verification = await userDirectory.VerifyConnectionAsync(
            lower, higher, cancellationToken);

        if (verification == ConnectionVerificationResult.Unknown)
        {
            // Fail closed: never cache an unknown answer, and never allow a
            // connection that neither the projection nor UserService confirms.
            logger.LogWarning(
                "Connection verification for {Lower}/{Higher} was unavailable; denying access.",
                lower,
                higher);
            return false;
        }

        var connected = verification == ConnectionVerificationResult.Connected;

        cache.Set(
            cacheKey,
            connected,
            connected ? PositiveCacheDuration : NegativeCacheDuration);

        return connected;
    }
}
