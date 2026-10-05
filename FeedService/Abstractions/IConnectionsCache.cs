using System;

namespace FeedService.Abstractions;

public interface IConnectionsCache
{
    /// <summary>
    /// Adds a connection between two users in the cache.
    /// </summary>
    Task AddConnectionAsync(
        string userId1,
        string userId2,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a connection between two users from the cache.
    /// </summary>
    Task RemoveConnectionAsync(
        string userId1,
        string userId2,
        CancellationToken ct = default);

    /// <summary>
    /// Replaces the cached connections for a user with a new collection.
    /// </summary>
    Task ReplaceConnectionsAsync(
        string userId,
        IReadOnlyCollection<string> connections,
        CancellationToken ct = default);




    /// <summary>
    /// Gets the list of connections for a specified user.
    /// </summary>
    Task<IReadOnlyList<string>> GetConnectionsAsync(
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Determines whether two users are connected.
    /// </summary>
    Task<bool> AreConnectedAsync(
        string userId1,
        string userId2,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the total number of connections for a user.
    /// </summary>
    Task<long> GetConnectionCountAsync(
        string userId,
        CancellationToken ct = default);



    /// <summary>
    /// Clears the cached connections for a user.
    /// </summary>
    Task ClearAsync(
    string userId,
     CancellationToken ct = default);
}