using FeedService.Abstractions;
using FeedService.Helpers.RedisConfiguration;
using FeedService.Helpers.RedisConfigurations;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FeedService.Redis;

public class RedisConnectionsCache : IConnectionsCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly FeedOptions _options;
    private readonly ILogger<RedisConnectionsCache> _logger;

    private static readonly TimeSpan ConnectionTtl = TimeSpan.FromDays(7);

    public RedisConnectionsCache(
        IConnectionMultiplexer redis,
        IOptions<FeedOptions> options,
        ILogger<RedisConnectionsCache> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
    }

    public async Task AddConnectionAsync(
        string userId1,
        string userId2,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        var key1 = RedisKeys.Connections(userId1);
        var key2 = RedisKeys.Connections(userId2);

        var batch = db.CreateBatch();
        var task1 = batch.SetAddAsync(key1, userId2);
        var task2 = batch.SetAddAsync(key2, userId1);
        batch.Execute();

        await Task.WhenAll(task1, task2);

        await db.KeyExpireAsync(key1, ConnectionTtl);
        await db.KeyExpireAsync(key2, ConnectionTtl);

        _logger.LogDebug(
            "Connection added: {User1} <-> {User2}", userId1, userId2);
    }

    public async Task RemoveConnectionAsync(
        string userId1,
        string userId2,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        var key1 = RedisKeys.Connections(userId1);
        var key2 = RedisKeys.Connections(userId2);

        var batch = db.CreateBatch();
        var task1 = batch.SetRemoveAsync(key1, userId2);
        var task2 = batch.SetRemoveAsync(key2, userId1);
        batch.Execute();

        await Task.WhenAll(task1, task2);

        _logger.LogDebug(
            "Connection removed: {User1} <-> {User2}", userId1, userId2);
    }

    public async Task ReplaceConnectionsAsync(
        string userId,
        IReadOnlyCollection<string> connections,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Connections(userId);

        var batch = db.CreateBatch();

        var deleteTask = batch.KeyDeleteAsync(key);
        batch.Execute();
        await deleteTask;

        if (connections.Count == 0)
        {
            _logger.LogInformation(
                "Connections replaced for user {UserId} — no connections",
                userId);
            return;
        }

        var values = connections
            .Select(c => (RedisValue)c)
            .ToArray();

        await db.SetAddAsync(key, values);
        await db.KeyExpireAsync(key, ConnectionTtl);

        _logger.LogInformation(
            "Connections replaced for user {UserId} — {Count} connections",
            userId, connections.Count);
    }


    public async Task<IReadOnlyList<string>> GetConnectionsAsync(
        string userId,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Connections(userId);

        var members = await db.SetMembersAsync(key);

        return members
            .Where(m => !m.IsNull)
            .Select(m => ((string)m)!)
            .ToList();
    }

    public async Task<bool> AreConnectedAsync(
        string userId1,
        string userId2,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Connections(userId1);

        return await db.SetContainsAsync(key, userId2);
    }

    public async Task<long> GetConnectionCountAsync(
        string userId,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Connections(userId);

        return await db.SetLengthAsync(key);
    }

    public async Task ClearAsync(
        string userId,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Connections(userId);

        await db.KeyDeleteAsync(key);

        _logger.LogInformation(
            "Connections cache cleared for user {UserId}", userId);
    }
}