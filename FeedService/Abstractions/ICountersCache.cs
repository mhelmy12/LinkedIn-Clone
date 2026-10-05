using System;
using FeedService.Helpers;

namespace FeedService.Abstractions;

public interface ICountersCache
{
    /// <summary>
    /// Increments the reaction count for the specified post by the given delta.
    /// </summary>
    Task IncrementReactionsAsync(
        long postId,
        long delta = 1,
        CancellationToken ct = default);

    /// <summary>
    /// Increments the comment count for the specified post by the given delta.
    /// </summary>
    Task IncrementCommentsAsync(
        long postId,
        long delta = 1,
        CancellationToken ct = default);

    /// <summary>
    /// Increments the repost count for the specified post by the given delta.
    /// </summary>
    Task IncrementRepostsAsync(
        long postId,
        long delta = 1,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the counters for the specified post.
    /// </summary>
    Task<PostCounters> GetAsync(
        long postId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the counters for multiple posts in a single operation.
    /// </summary>
    Task<IReadOnlyList<PostCounters>> GetManyAsync(
        IReadOnlyList<long> postIds,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes the counters entry for the specified post.
    /// </summary>
    Task DeleteAsync(
        long postId,
        CancellationToken ct = default);
}