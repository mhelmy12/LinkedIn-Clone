using System;
using FeedService.Abstractions;
using FeedService.Helpers.RedisConfiguration;
using FeedService.Helpers.RedisConfigurations;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FeedService.Redis;

public class RedisFeedCache : IFeedCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly FeedOptions _options;
    private readonly ILogger<RedisFeedCache> _logger;

    private static readonly TimeSpan FeedTtl = TimeSpan.FromDays(30);

    public RedisFeedCache(
        IConnectionMultiplexer redis,
        IOptions<FeedOptions> options,
        ILogger<RedisFeedCache> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
    }

    public async Task AddToFeedAsync(string userId, long postId, double score, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Feed(userId);

        await db.SortedSetAddAsync(key, postId, score);

        // renew the TTL for the feed key to ensure it doesn't expire too soon
        await db.KeyExpireAsync(key, FeedTtl);

        // Trim the feed to the maximum size after adding a new post
        await TrimFeedAsync(userId, _options.MaxFeedSize, ct);
    }

    public async Task AddManyToFeedAsync(string userId, IReadOnlyList<(long PostId, double Score)> entries, CancellationToken ct = default)
    {
        if (entries.Count == 0) return;

        var db = _redis.GetDatabase();
        var key = RedisKeys.Feed(userId);

        // Convert the entries to Redis SortedSetEntry format
        var redisEntries = entries
            .Select(e => new SortedSetEntry(e.PostId, e.Score))
            .ToArray();

        await db.SortedSetAddAsync(key, redisEntries);

        await db.KeyExpireAsync(key, FeedTtl);

        await TrimFeedAsync(userId, _options.MaxFeedSize, ct);
    }

    public async Task RemoveFromFeedAsync(string userId, long postId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Feed(userId);

        await db.SortedSetRemoveAsync(key, postId);
    }

    public async Task TrimFeedAsync(string userId, int maxSize, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Feed(userId);

        await db.SortedSetRemoveRangeByRankAsync(
            key,
            start: 0,
            stop: -(maxSize + 1));
    }

    public async Task<IReadOnlyList<FeedEntry>> GetFeedPageAsync(string userId, FeedCursor? cursor, int limit, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Feed(userId);

        const int CursorSlack = 10;
        var take = limit + CursorSlack;

        RedisValue[] elements;
        double[] scores;

        if (cursor is null)
        {
            var entries = await db.SortedSetRangeByRankWithScoresAsync(
                key,
                start: 0,
                stop: take - 1,
                order: Order.Descending);

            return entries
                .Select(e => new FeedEntry(
                    PostId: (long)e.Element,
                    Score: e.Score))
                .Take(limit)
                .ToList();
        }

        var scoreEntries = await db.SortedSetRangeByScoreWithScoresAsync(
            key,
            start: double.NegativeInfinity,
            stop: cursor.Score,
            exclude: Exclude.Start,
            order: Order.Descending,
            take: take);

        var result = new List<FeedEntry>(limit);

        foreach (var entry in scoreEntries)
        {
            var entryScore = entry.Score;
            var entryPostId = (long)entry.Element;

            if (entryScore < cursor.Score)
            {
                result.Add(new FeedEntry(entryPostId, entryScore));
            }
            else if (entryScore == cursor.Score && entryPostId < cursor.PostId)
            {
                result.Add(new FeedEntry(entryPostId, entryScore));
            }

            if (result.Count >= limit)
                break;
        }

        if (result.Count < limit)
        {
            var sameScoreEntries = await db.SortedSetRangeByScoreWithScoresAsync(
                key,
                start: cursor.Score,
                stop: cursor.Score,
                order: Order.Descending,
                take: take);

            foreach (var entry in sameScoreEntries)
            {
                var entryPostId = (long)entry.Element;

                if (entryPostId >= cursor.PostId)
                    continue;

                if (result.Any(r => r.PostId == entryPostId))
                    continue;

                result.Add(new FeedEntry(entryPostId, entry.Score));

                if (result.Count >= limit)
                    break;
            }
        }

        return result;
    }

    public async Task<long> GetFeedCountAsync(string userId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Feed(userId);

        return await db.SortedSetLengthAsync(key);
    }

    public async Task ClearFeedAsync(string userId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Feed(userId);

        await db.KeyDeleteAsync(key);

        _logger.LogInformation("Feed cleared for user {UserId}", userId);
    }
}
