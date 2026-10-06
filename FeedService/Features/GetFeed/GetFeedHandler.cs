using System;

namespace FeedService.Features.GetFeed;

using FeedService.Abstractions;
using FeedService.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Helpers;
using Shared.Services.CurrentUserService;

public class GetFeedQueryHandler(
    IFeedCache _feedCache,
    ISnapshotStore _snapshotStore,
    ICountersCache _countersCache,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
    ILogger<GetFeedQueryHandler> _logger
) : ResponseHandler, IRequestHandler<GetFeedQuery, Response<GetFeedResponse>>
{
    public async Task<Response<GetFeedResponse>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {


        var userId = _currentUserService.GetCurrentUserId();
        if (userId is null)
            return Unauthorized<GetFeedResponse>("User is not authenticated.");


        FeedCursor? cursor = null;

        if (!string.IsNullOrEmpty(request.Cursor))
        {
            if (!FeedCursorCodec.TryDecode(request.Cursor, out var decoded))
                return BadRequest<GetFeedResponse>("Invalid cursor.");

            cursor = new FeedCursor(decoded!.Score, decoded.PostId);
        }

        var entries = await _feedCache.GetFeedPageAsync(
            userId,
            cursor,
            request.Limit,
            cancellationToken);

        if (entries.Count == 0)
        {
            return Success(new GetFeedResponse(
                Items: Array.Empty<FeedItemDto>(),
                NextCursor: null,
                HasMore: false));
        }

        var postIds = entries.Select(e => e.PostId).ToList();

        var snapshots = await _snapshotStore.GetManyAsync(
            postIds, cancellationToken);

        var snapshotMap = snapshots.ToDictionary(s => s.PostId);

        var counters = await _countersCache.GetManyAsync(
            postIds, cancellationToken);

        var countersMap = counters.ToDictionary(c => c.PostId);

        var items = new List<FeedItemDto>(entries.Count);

        foreach (var entry in entries)
        {
            if (!snapshotMap.TryGetValue(entry.PostId, out var snapshot))
            {
                _logger.LogDebug(
                    "Snapshot not found for post {PostId} in feed of user {UserId}",
                    entry.PostId, userId);

                continue;
            }

            countersMap.TryGetValue(entry.PostId, out var c);

            items.Add(new FeedItemDto(
                PostId: snapshot.PostId,
                AuthorId: snapshot.AuthorId,
                AuthorDisplayName: snapshot.AuthorDisplayName,
                AuthorProfileImageKey: snapshot.AuthorProfileImageKey,
                AuthorHeadline: snapshot.AuthorHeadline,
                Content: snapshot.Content,
                Visibility: snapshot.Visibility,
                IsPureRepost: snapshot.IsPureRepost,
                RepostOfPostId: snapshot.RepostOfPostId,
                RepostOfContent: snapshot.RepostOfContent,
                RepostOfAuthorId: snapshot.RepostOfAuthorId,
                RepostOfAuthorDisplayName: snapshot.RepostOfAuthorDisplayName,
                Hashtags: snapshot.Hashtags,
                MentionedUserIds: snapshot.MentionedUserIds,
                Counters: new FeedCountersDto(
                    Reactions: c?.Reactions ?? 0,
                    Comments: c?.Comments ?? 0,
                    Reposts: c?.Reposts ?? 0),
                CreatedAt: snapshot.CreatedAt,
                UpdatedAt: snapshot.UpdatedAt));
        }

        string? nextCursor = null;

        if (entries.Count == request.Limit)
        {
            var last = entries[^1];
            nextCursor = FeedCursorCodec.Encode(new FeedCursorData
            {
                Score = last.Score,
                PostId = last.PostId
            });
        }

        var hasMore = nextCursor is not null;

        _logger.LogInformation(
            "GetFeed for user {UserId}: returned {Count} items (hasMore: {HasMore})",
            userId, items.Count, hasMore);

        return Success(new GetFeedResponse(
            Items: items,
            NextCursor: nextCursor,
            HasMore: hasMore));
    }
}