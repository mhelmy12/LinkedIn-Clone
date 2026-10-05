using System;
using FeedService.Models;

namespace FeedService.Abstractions;



public interface IUserSummaryStore
{

    /// <summary>
    /// Inserts or updates a user summary in the store.
    /// </summary>
    Task UpsertAsync(
        UserSummary summary,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user summary by user ID.    
    /// </summary>
    Task<UserSummary?> GetAsync(
        string userId,
        CancellationToken ct = default);



    /// <summary>
    /// Retrieves multiple user summaries by their user IDs.
    /// </summary>
    Task<IReadOnlyList<UserSummary>> GetManyAsync(
        IReadOnlyList<string> userIds,
        CancellationToken ct = default);
}
