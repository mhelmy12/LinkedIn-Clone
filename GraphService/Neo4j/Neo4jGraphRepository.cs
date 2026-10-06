using System;

namespace GraphService.Neo4j;

using global::Neo4j.Driver;
using GraphService.Abstractions;
using GraphService.Models;
using Microsoft.Extensions.Logging;

public class Neo4jGraphRepository : IGraphRepository
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jGraphRepository> _logger;

    public Neo4jGraphRepository(
        IDriver driver,
        ILogger<Neo4jGraphRepository> logger)
    {
        _driver = driver;
        _logger = logger;
    }


    public async Task UpsertUserAsync(
        UserNode user,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();

        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(@"
                MERGE (u:User {userId: $userId})
                ON CREATE SET
                    u.displayName = $displayName,
                    u.profileImageKey = $profileImageKey,
                    u.headline = $headline,
                    u.createdAt = datetime(),
                    u.updatedAt = datetime()
                ON MATCH SET
                    u.displayName = $displayName,
                    u.profileImageKey = $profileImageKey,
                    u.headline = $headline,
                    u.updatedAt = datetime()",
                new
                {
                    userId = user.UserId,
                    displayName = user.DisplayName,
                    profileImageKey = user.ProfileImageKey,
                    headline = user.Headline
                });
        });

        _logger.LogDebug("User {UserId} upserted in Neo4j", user.UserId);
    }

    public async Task UpdateUserProfileAsync(
        string userId,
        string displayName,
        string? profileImageKey,
        string? headline,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();

        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(@"
                MATCH (u:User {userId: $userId})
                SET u.displayName = $displayName,
                    u.profileImageKey = $profileImageKey,
                    u.headline = $headline,
                    u.updatedAt = datetime()",
                new
                {
                    userId,
                    displayName,
                    profileImageKey,
                    headline
                });
        });

        _logger.LogDebug("User {UserId} profile updated in Neo4j", userId);
    }

    public async Task DeleteUserAsync(
        string userId,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();

        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(@"
                MATCH (u:User {userId: $userId})
                DETACH DELETE u",
                new { userId });
        });

        _logger.LogInformation("User {UserId} deleted from Neo4j", userId);
    }


    public async Task AddConnectionAsync(
        string userId1,
        string userId2,
        DateTime since,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();

        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(@"
                MERGE (a:User {userId: $userId1})
                MERGE (b:User {userId: $userId2})
                MERGE (a)-[r1:CONNECTED_TO]->(b)
                ON CREATE SET r1.since = datetime($since)
                MERGE (b)-[r2:CONNECTED_TO]->(a)
                ON CREATE SET r2.since = datetime($since)",
                new
                {
                    userId1,
                    userId2,
                    since = since.ToString("O")
                });
        });

        _logger.LogDebug(
            "Connection added in Neo4j: {User1} ↔ {User2}", userId1, userId2);
    }

    public async Task RemoveConnectionAsync(
        string userId1,
        string userId2,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();

        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(@"
                MATCH (a:User {userId: $userId1})-[r:CONNECTED_TO]-(b:User {userId: $userId2})
                DELETE r",
                new { userId1, userId2 });
        });

        _logger.LogDebug(
            "Connection removed from Neo4j: {User1} <-> {User2}", userId1, userId2);
    }



    public async Task<IReadOnlyList<MutualConnectionResult>> GetMutualConnectionsAsync(
        string userId1,
        string userId2,
        int limit,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();

        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(@"
                MATCH (a:User {userId: $userId1})-[:CONNECTED_TO]->(mutual)
                      <-[:CONNECTED_TO]-(b:User {userId: $userId2})
                RETURN mutual.userId AS userId,
                       mutual.displayName AS displayName,
                       mutual.profileImageKey AS profileImageKey,
                       mutual.headline AS headline
                LIMIT $limit",
                new { userId1, userId2, limit });

            return await cursor.ToListAsync(record => new MutualConnectionResult
            {
                UserId = record["userId"].As<string>(),
                DisplayName = record["displayName"].As<string>(),
                ProfileImageKey = record["profileImageKey"].As<string?>(),
                Headline = record["headline"].As<string?>()
            });
        });

        return result;
    }

    public async Task<IReadOnlyList<SuggestionResult>> GetSuggestionsAsync(
        string userId,
        int limit,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();

        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(@"
                MATCH (me:User {userId: $userId})-[:CONNECTED_TO]->(friend)
                      -[:CONNECTED_TO]->(suggestion)
                WHERE NOT (me)-[:CONNECTED_TO]->(suggestion)
                  AND me <> suggestion
                WITH suggestion,
                     COUNT(DISTINCT friend) AS mutualCount,
                     COLLECT(DISTINCT friend.userId)[0..3] AS mutualFriendIds
                RETURN suggestion.userId AS userId,
                       suggestion.displayName AS displayName,
                       suggestion.profileImageKey AS profileImageKey,
                       suggestion.headline AS headline,
                       mutualCount,
                       mutualFriendIds
                ORDER BY mutualCount DESC
                LIMIT $limit",
                new { userId, limit });

            return await cursor.ToListAsync(record => new SuggestionResult
            {
                UserId = record["userId"].As<string>(),
                DisplayName = record["displayName"].As<string>(),
                ProfileImageKey = record["profileImageKey"].As<string?>(),
                Headline = record["headline"].As<string?>(),
                MutualCount = record["mutualCount"].As<int>(),
                MutualFriendIds = record["mutualFriendIds"]
                    .As<List<string>>()
                    .Select(x => Convert.ToString(x))
                    .ToList()
            });
        });

        return result;
    }

    public async Task<int?> GetDegreesOfSeparationAsync(
        string fromUserId,
        string toUserId,
        int maxDepth,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();

        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync($@"
                MATCH (a:User {{userId: $fromUserId}}), (b:User {{userId: $toUserId}})
                MATCH path = shortestPath((a)-[:CONNECTED_TO*..{maxDepth}]-(b))
                RETURN length(path) AS degrees",
                new { fromUserId, toUserId });

            var record = await cursor.SingleAsync();

            return record is null ? null : (int?)record["degrees"].As<int>();
        });

        return result;
    }
}