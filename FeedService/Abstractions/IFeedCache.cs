using System;

namespace FeedService.Abstractions;

public interface IFeedCache
{
    // Write Operations (fan-out)
    /// <summary>Adds a post to a user's feed.</summary>
    Task AddToFeedAsync(
        long userId,
        long postId,
        double score,
        CancellationToken ct = default);

    /// <summary>Adds multiple posts to a user's feed.</summary>
    Task AddManyToFeedAsync(
        long userId,
        IReadOnlyList<(long PostId, double Score)> entries,
        CancellationToken ct = default);

    /// <summary>Removes a post from a user's feed.</summary>
    Task RemoveFromFeedAsync(
        long userId,
        long postId,
        CancellationToken ct = default);
    /// <summary>Trims a user's feed to the specified maximum size.</summary>
    Task TrimFeedAsync(
        long userId,
        int maxSize,
        CancellationToken ct = default);


    // Read Operations (GetFeed)
    /// <summary>Retrieves a page of entries from a user's feed.</summary>
    Task<IReadOnlyList<FeedEntry>> GetFeedPageAsync(
        long userId,
        FeedCursor? cursor,
        int limit,
        CancellationToken ct = default);

    /// <summary>Gets the number of entries in a user's feed.</summary>
    Task<long> GetFeedCountAsync(
        long userId,
        CancellationToken ct = default);

    /// <summary>Removes all entries from a user's feed.</summary>
    Task ClearFeedAsync(
        long userId,
        CancellationToken ct = default);
}

public record FeedEntry(
    long PostId,
    double Score);

public record FeedCursor(
    double Score,
    long PostId);