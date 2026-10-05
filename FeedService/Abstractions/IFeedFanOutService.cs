using FeedService.Models;
namespace FeedService.Abstractions;


public interface IFeedFanOutService
{
    /// <summary>
    /// Fan-out post for the author and all their connections.
    /// </summary>
    Task FanOutAsync(
        PostSnapshot snapshot,
        CancellationToken ct = default);

    /// <summary>
    /// Fan-out posts for a specific user when they accept a connection.
    /// </summary>
    Task FanOutToUserAsync(
        string targetUserId,
        IReadOnlyList<PostSnapshot> snapshots,
        CancellationToken ct = default);

    /// <summary>
    /// Remove a post from all feeds when it is deleted.
    /// </summary>
    Task RemoveFromFeedsAsync(
        long postId,
        string authorId,
        CancellationToken ct = default);
}
