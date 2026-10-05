using System;
using FeedService.Models;

namespace FeedService.Abstractions;

public interface ISnapshotStore
{
    /// <summary>
    /// Saves a new post snapshot.
    /// </summary>
    Task SaveAsync(
        PostSnapshot snapshot,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing post snapshot.
    /// </summary>
    Task UpdateAsync(
        PostSnapshot snapshot,
        CancellationToken ct = default);


    /// <summary>
    /// Marks a post snapshot as deleted.
    /// </summary>
    Task MarkDeletedAsync(
        long postId,
        CancellationToken ct = default);




    /// <summary>
    /// Gets a single post snapshot by its ID.
    /// </summary>
    Task<PostSnapshot?> GetAsync(
        long postId,
        CancellationToken ct = default);


    /// <summary>
    /// Gets multiple post snapshots by their IDs.
    /// </summary>
    Task<IReadOnlyList<PostSnapshot>> GetManyAsync(
        IReadOnlyList<long> postIds,
        CancellationToken ct = default);


    /// <summary>
    /// Gets the most recent post snapshots for a specific author for initial fan-out.
    /// </summary>
    Task<IReadOnlyList<PostSnapshot>> GetRecentByAuthorAsync(
        string authorId,
        int limit,
        CancellationToken ct = default);
}