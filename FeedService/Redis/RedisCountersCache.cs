using System;

namespace FeedService.Redis;

using FeedService.Abstractions;
using FeedService.Helpers;
using FeedService.Helpers.RedisConfigurations;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
public class RedisCountersCache : ICountersCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCountersCache> _logger;

    public RedisCountersCache(
        IConnectionMultiplexer redis,
        ILogger<RedisCountersCache> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public Task IncrementReactionsAsync(
        long postId,
        long delta = 1,
        CancellationToken ct = default)
        => IncrementAsync(postId, RedisKeys.CounterFields.Reactions, delta);

    public Task IncrementCommentsAsync(
        long postId,
        long delta = 1,
        CancellationToken ct = default)
        => IncrementAsync(postId, RedisKeys.CounterFields.Comments, delta);

    public Task IncrementRepostsAsync(
        long postId,
        long delta = 1,
        CancellationToken ct = default)
        => IncrementAsync(postId, RedisKeys.CounterFields.Reposts, delta);

    private async Task IncrementAsync(
        long postId,
        string field,
        long delta)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.PostCounters(postId);

        try
        {
            var newValue = await db.HashIncrementAsync(key, field, delta);

            _logger.LogDebug(
                "Counter incremented: {Key}.{Field} by {Delta} → {NewValue}",
                key, field, delta, newValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to increment counter {Key}.{Field} by {Delta}",
                key, field, delta);

            throw;
        }
    }


    public async Task<PostCounters> GetAsync(
        long postId,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.PostCounters(postId);

        try
        {
            var values = await db.HashGetAsync(key,
            [
                RedisKeys.CounterFields.Reactions,
                RedisKeys.CounterFields.Comments,
                RedisKeys.CounterFields.Reposts
            ]);

            return new PostCounters(
                PostId: postId,
                Reactions: ParseLong(values[0]),
                Comments: ParseLong(values[1]),
                Reposts: ParseLong(values[2]));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to read counters for post {PostId}. Returning zeros.",
                postId);

            return PostCounters.Empty(postId);
        }
    }

    public async Task<IReadOnlyList<PostCounters>> GetManyAsync(
        IReadOnlyList<long> postIds,
        CancellationToken ct = default)
    {
        if (postIds.Count == 0)
            return Array.Empty<PostCounters>();

        var db = _redis.GetDatabase();

        try
        {
            var batch = db.CreateBatch();
            var fields = new RedisValue[]
            {
                RedisKeys.CounterFields.Reactions,
                RedisKeys.CounterFields.Comments,
                RedisKeys.CounterFields.Reposts
            };

            var tasks = new Task<RedisValue[]>[postIds.Count];

            for (int i = 0; i < postIds.Count; i++)
            {
                var key = RedisKeys.PostCounters(postIds[i]);
                tasks[i] = batch.HashGetAsync(key, fields);
            }

            batch.Execute();

            var responses = await Task.WhenAll(tasks);

            var result = new List<PostCounters>(postIds.Count);

            for (int i = 0; i < postIds.Count; i++)
            {
                var values = responses[i];
                result.Add(new PostCounters(
                    PostId: postIds[i],
                    Reactions: ParseLong(values[0]),
                    Comments: ParseLong(values[1]),
                    Reposts: ParseLong(values[2])));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to read counters for {Count} posts. Returning zeros.",
                postIds.Count);

            return postIds.Select(PostCounters.Empty).ToList();
        }
    }


    public async Task DeleteAsync(
        long postId,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.PostCounters(postId);

        await db.KeyDeleteAsync(key);

        _logger.LogDebug("Counters deleted for post {PostId}", postId);
    }
    private static long ParseLong(RedisValue value)
    {
        if (value.IsNull)
            return 0;

        return (long)value;
    }
}