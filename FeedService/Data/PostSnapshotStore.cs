

using FeedService.Abstractions;
using FeedService.Models;
using Microsoft.EntityFrameworkCore;
namespace FeedService.Data;

public class PostSnapshotStore : ISnapshotStore
{
    private readonly FeedDbContext _db;
    private readonly ILogger<PostSnapshotStore> _logger;

    public PostSnapshotStore(
        FeedDbContext db,
        ILogger<PostSnapshotStore> logger)
    {
        _db = db;
        _logger = logger;
    }



    public async Task SaveAsync(
        PostSnapshot snapshot,
        CancellationToken ct = default)
    {
        var exists = await _db.PostSnapshots
            .AnyAsync(p => p.PostId == snapshot.PostId, ct);

        if (exists)
        {
            _logger.LogWarning(
                "PostSnapshot {PostId} already exists — updating instead",
                snapshot.PostId);

            _db.PostSnapshots.Update(snapshot);
        }
        else
        {
            _db.PostSnapshots.Add(snapshot);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogDebug(
            "PostSnapshot {PostId} saved (author: {AuthorId})",
            snapshot.PostId, snapshot.AuthorId);
    }

    public async Task UpdateAsync(
        PostSnapshot snapshot,
        CancellationToken ct = default)
    {
        var existing = await _db.PostSnapshots
            .FirstOrDefaultAsync(p => p.PostId == snapshot.PostId, ct);

        if (existing is null)
        {
            _logger.LogWarning(
                "PostSnapshot {PostId} not found — inserting instead",
                snapshot.PostId);

            _db.PostSnapshots.Add(snapshot);
            await _db.SaveChangesAsync(ct);
            return;
        }

        existing.Content = snapshot.Content;
        existing.Visibility = snapshot.Visibility;
        existing.Hashtags = snapshot.Hashtags;
        existing.MentionedUserIds = snapshot.MentionedUserIds;
        existing.UpdatedAt = snapshot.UpdatedAt;

        await _db.SaveChangesAsync(ct);

        _logger.LogDebug("PostSnapshot {PostId} updated", snapshot.PostId);
    }

    public async Task MarkDeletedAsync(
        long postId,
        CancellationToken ct = default)
    {
        var affected = await _db.PostSnapshots
            .Where(p => p.PostId == postId && !p.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsDeleted, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                ct);

        if (affected == 0)
        {
            _logger.LogDebug(
                "PostSnapshot {PostId} not found or already deleted",
                postId);
        }
        else
        {
            _logger.LogDebug("PostSnapshot {PostId} marked as deleted", postId);
        }
    }


    public async Task<PostSnapshot?> GetAsync(
        long postId,
        CancellationToken ct = default)
    {
        return await _db.PostSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PostId == postId, ct);
    }

    public async Task<IReadOnlyList<PostSnapshot>> GetManyAsync(
        IReadOnlyList<long> postIds,
        CancellationToken ct = default)
    {
        if (postIds.Count == 0)
            return Array.Empty<PostSnapshot>();

        var snapshots = await _db.PostSnapshots
            .AsNoTracking()
            .Where(p => postIds.Contains(p.PostId))
            .ToListAsync(ct);

        var map = snapshots.ToDictionary(s => s.PostId);

        return postIds
            .Where(id => map.ContainsKey(id))
            .Select(id => map[id])
            .ToList();
    }

    public async Task<IReadOnlyList<PostSnapshot>> GetRecentByAuthorAsync(
        string authorId,
        int limit,
        CancellationToken ct = default)
    {
        return await _db.PostSnapshots
            .AsNoTracking()
            .Where(p => p.AuthorId == authorId)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.PostId)
            .Take(limit)
            .ToListAsync(ct);
    }
}