using System;

namespace GraphService.Abstractions;

using GraphService.Models;
public interface IGraphRepository
{
    Task UpsertUserAsync(UserNode user, CancellationToken ct = default);

    Task UpdateUserProfileAsync(
        string userId,
        string displayName,
        string? profileImageKey,
        string? headline,
        CancellationToken ct = default);

    Task DeleteUserAsync(string userId, CancellationToken ct = default);

    Task AddConnectionAsync(
        string userId1,
        string userId2,
        DateTime since,
        CancellationToken ct = default);

    Task RemoveConnectionAsync(
        string userId1,
        string userId2,
        CancellationToken ct = default);


    Task<IReadOnlyList<MutualConnectionResult>> GetMutualConnectionsAsync(
        string userId1,
        string userId2,
        int limit,
        CancellationToken ct = default);

    Task<IReadOnlyList<SuggestionResult>> GetSuggestionsAsync(
        string userId,
        int limit,
        CancellationToken ct = default);

    Task<int?> GetDegreesOfSeparationAsync(
        string fromUserId,
        string toUserId,
        int maxDepth,
        CancellationToken ct = default);
}