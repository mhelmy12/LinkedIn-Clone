namespace ChatService.Services;

/// <summary>
/// Normalizes a participant pair into ordinal min/max order. Every write that
/// keys a conversation or connection projection must use this so <c>(A,B)</c>
/// and <c>(B,A)</c> resolve to the same row (FR-003/SC-006).
/// </summary>
public static class PairNormalizer
{
    public static (string Lower, string Higher) Normalize(string userId1, string userId2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId1);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId2);

        if (string.Equals(userId1, userId2, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A pair requires two distinct user ids.", nameof(userId2));
        }

        return string.CompareOrdinal(userId1, userId2) < 0
            ? (userId1, userId2)
            : (userId2, userId1);
    }
}
