using FeedService.Abstractions;
using FeedService.Models;
using Microsoft.EntityFrameworkCore;

namespace FeedService.Data;

public class UserSummaryStore : IUserSummaryStore
{
    private readonly FeedDbContext _db;
    private readonly ILogger<UserSummaryStore> _logger;

    public UserSummaryStore(
        FeedDbContext db,
        ILogger<UserSummaryStore> logger)
    {
        _db = db;
        _logger = logger;
    }


    public async Task UpsertAsync(
        UserSummary summary,
        CancellationToken ct = default)
    {
        var existing = await _db.UserSummaries
            .FirstOrDefaultAsync(u => u.UserId == summary.UserId, ct);

        if (existing is null)
        {
            _db.UserSummaries.Add(summary);

            _logger.LogDebug(
                "UserSummary {UserId} inserted", summary.UserId);
        }
        else
        {
            if (existing.UpdatedAt > summary.UpdatedAt)
            {
                _logger.LogDebug(
                    "Skipping stale UserSummary for {UserId} (event: {EventTs}, stored: {StoredTs})",
                    summary.UserId, summary.UpdatedAt, existing.UpdatedAt);
                return;
            }

            existing.DisplayName = summary.DisplayName;
            existing.ProfileImageKey = summary.ProfileImageKey;
            existing.Headline = summary.Headline;
            existing.UpdatedAt = summary.UpdatedAt;

            _logger.LogDebug(
                "UserSummary {UserId} updated", summary.UserId);
        }

        await _db.SaveChangesAsync(ct);
    }


    public async Task<UserSummary?> GetAsync(
        string userId,
        CancellationToken ct = default)
    {
        return await _db.UserSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);
    }

    public async Task<IReadOnlyList<UserSummary>> GetManyAsync(
        IReadOnlyList<string> userIds,
        CancellationToken ct = default)
    {
        if (userIds.Count == 0)
            return Array.Empty<UserSummary>();

        return await _db.UserSummaries
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToListAsync(ct);
    }
}
