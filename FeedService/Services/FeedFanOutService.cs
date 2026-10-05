using FeedService.Abstractions;
using FeedService.Helpers.RedisConfiguration;
using FeedService.Models;
using Microsoft.Extensions.Options;


namespace FeedService.Services;
public class FeedFanOutService : IFeedFanOutService
{
    private readonly IFeedCache _feedCache;
    private readonly IConnectionsCache _connectionsCache;
    private readonly ILogger<FeedFanOutService> _logger;

    public FeedFanOutService(
        IFeedCache feedCache,
        IConnectionsCache connectionsCache,
        ILogger<FeedFanOutService> logger)
    {
        _feedCache = feedCache;
        _connectionsCache = connectionsCache;
        _logger = logger;
    }

    public async Task FanOutAsync(
        PostSnapshot snapshot,
        CancellationToken ct = default)
    {
        var score = ToScore(snapshot.CreatedAt);

        var connections = await _connectionsCache
            .GetConnectionsAsync(snapshot.AuthorId, ct);

        var recipients = new HashSet<string>(connections)
        {
            snapshot.AuthorId
        };

        _logger.LogInformation(
            "Fan-out post {PostId} to {Count} feeds (author: {AuthorId}, score: {Score})",
            snapshot.PostId, recipients.Count, snapshot.AuthorId, score);

        // 3. Fan-out — parallel across recipients
        var tasks = recipients.Select(userId =>
            AddToFeedSafeAsync(userId, snapshot.PostId, score, ct));

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Fan-out completed for post {PostId}", snapshot.PostId);
    }

    public async Task FanOutToUserAsync(
        string targetUserId,
        IReadOnlyList<PostSnapshot> snapshots,
        CancellationToken ct = default)
    {
        if (snapshots.Count == 0) return;

        var entries = snapshots
            .Select(s => (s.PostId, ToScore(s.CreatedAt)))
            .ToList();

        await _feedCache.AddManyToFeedAsync(targetUserId, entries, ct);

        _logger.LogInformation(
            "Added {Count} posts to feed of user {UserId}",
            entries.Count, targetUserId);
    }

    public async Task RemoveFromFeedsAsync(
        long postId,
        string authorId,
        CancellationToken ct = default)
    {
        var connections = await _connectionsCache
            .GetConnectionsAsync(authorId, ct);

        var recipients = new HashSet<string>(connections) { authorId };

        var tasks = recipients.Select(userId =>
            RemoveFromFeedSafeAsync(userId, postId, ct));

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Removed post {PostId} from {Count} feeds",
            postId, recipients.Count);
    }

    private async Task AddToFeedSafeAsync(
        string userId,
        long postId,
        double score,
        CancellationToken ct)
    {
        try
        {
            await _feedCache.AddToFeedAsync(userId, postId, score, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to add post {PostId} to feed of user {UserId}",
                postId, userId);

            throw;
        }
    }

    private async Task RemoveFromFeedSafeAsync(
        string userId,
        long postId,
        CancellationToken ct)
    {
        try
        {
            await _feedCache.RemoveFromFeedAsync(userId, postId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to remove post {PostId} from feed of user {UserId}",
                postId, userId);

            throw;
        }
    }

    private static double ToScore(DateTime createdAt)
        => new DateTimeOffset(createdAt, TimeSpan.Zero)
            .ToUnixTimeMilliseconds();
}
