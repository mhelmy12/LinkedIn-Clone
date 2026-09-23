using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PostService.Data;
using PostService.Helpers;
using PostService.Models;
using PostService.Services.CurrentUserService;
using Shared.Helpers;

namespace PostService.Features.GetPostReposts;

public class GetPostRepostsQueryHandler(
        PostDbContext _db,
        [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
        ILogger<GetPostRepostsQueryHandler> _logger
) : ResponseHandler, IRequestHandler<GetPostRepostsQuery, Response<GetPostRepostsQueryResponse>>
{
    public async Task<Response<GetPostRepostsQueryResponse>> Handle(
        GetPostRepostsQuery request,
        CancellationToken ct)
    {
        var currentUser = _currentUserService.GetCurrentUserId();

        //Parse cursor
        DateTime? cursorCreatedAt = null;
        long? cursorId = null;

        if (!string.IsNullOrEmpty(request.Cursor))
        {
            if (!CursorCodec.TryDecode(request.Cursor, out var dt, out var id))
                return BadRequest<GetPostRepostsQueryResponse>("Invalid cursor.");

            cursorCreatedAt = dt;
            cursorId = id;
        }

        // Verify original post exists + visible
        var originalPost = await _db.Posts
            .AsNoTracking()
            .Where(p => p.Id.ToString() == request.PostId && !p.IsDeleted)
            .Select(p => new
            {
                p.Id,
                p.AuthorId,
                p.Visibility
            })
            .FirstOrDefaultAsync(ct);

        if (originalPost is null)
            return NotFound<GetPostRepostsQueryResponse>("Post not found.");

        if (originalPost.Visibility == VisibilityType.Private
            && originalPost.AuthorId != currentUser)
        {
            return NotFound<GetPostRepostsQueryResponse>("Post not found.");
        }

        // Reposts of the original post
        var query = _db.Posts
            .AsNoTracking()
            .Where(p => p.RepostOfPostId.ToString() == request.PostId)
            .Where(p => !p.IsDeleted);

        // visibility filter
        if (currentUser != originalPost.AuthorId)
        {
            query = query.Where(p =>
                p.Visibility != VisibilityType.Private
                || p.AuthorId == currentUser);
        }

        // keyset cursor
        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            query = query.Where(p =>
                p.CreatedAt < cursorCreatedAt.Value
                || (p.CreatedAt == cursorCreatedAt.Value
                    && p.Id < cursorId.Value));
        }

        var rows = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(request.Limit + 1)
            .Select(p => new
            {
                p.Id,
                p.AuthorId,
                p.Content,
                p.CreatedAt,
                p.UpdatedAt,
                // Author = _db.UserSummaries
                //     .Where(u => u.UserId == p.AuthorId)
                //     .Select(u => new AuthorDto(
                //         u.UserId,
                //         u.DisplayName,
                //         u.ProfileImageKey,
                //         u.Headline))
                //     .FirstOrDefault(),

                Media = p.Media
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => new MediaDto(
                        m.MediaKey,
                        (int)m.MediaType,
                        m.DisplayOrder))
                    .ToList(),

                Mentions = p.Mentions
                    .OrderBy(m => m.StartIndex)
                    .Select(m => new
                    {
                        m.MentionedEntityId,
                        m.StartIndex,
                        m.Length,
                        // User = _db.UserSummaries
                        //     .Where(u => u.UserId == m.MentionedUserId)
                        //     .Select(u => new
                        //     {
                        //         u.DisplayName,
                        //         u.ProfileImageKey
                        //     })
                        //     .FirstOrDefault()
                    })
                    .ToList(),

                Hashtags = p.PostHashtags
                    .Select(ph => ph.NormalizedName)
                    .ToList()
            })
            .ToListAsync(ct);

        bool hasMore = rows.Count > request.Limit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        string? nextCursor = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextCursor = CursorCodec.Encode(last.CreatedAt, last.Id.ToString());
        }

        if (rows.Count == 0)
        {
            return Success(new GetPostRepostsQueryResponse(
                Items: new List<RepostSummaryDto>(),
                NextCursor: null,
                HasMore: false));
        }

        //Redis MGET (canonical ids)
        // Pure Repost → canonical = request.PostId (Original Post)
        // Quote → canonical = p.Id


        // var counterKeys = rows
        //     .Select(p => p.Content is null
        //         ? request.PostId
        //         : p.Id)
        //     .Distinct()
        //     .ToHashSet();

        // var countersMap = await _counters.GetManyAsync(counterKeys, ct);

        //Build DTOs
        var items = new List<RepostSummaryDto>(rows.Count);

        foreach (var row in rows)
        {
            var isPureRepost = row.Content is null;

            var canonicalPostId = isPureRepost
                ? request.PostId
                : row.Id.ToString();

            // countersMap.TryGetValue(canonicalPostId, out var counters);

            items.Add(new RepostSummaryDto(
                Id: row.Id.ToString(),
                CanonicalPostId: canonicalPostId,
                AuthorId: row.AuthorId,
                Author: null,
                Content: row.Content,
                IsPureRepost: isPureRepost,
                Media: row.Media,
                Mentions: row.Mentions
                    .Select(m => new MentionDto(
                        // DisplayName: m.User?.DisplayName
                        //     ?? $"User #{m.MentionedEntityId}",
                        // ProfileImageKey: m.User?.ProfileImageKey,
                        UserId: m.MentionedEntityId,
                        DisplayName: "null",
                        ProfileImageKey: "null",
                        StartIndex: m.StartIndex,
                        Length: m.Length))
                    .ToList(),
                Hashtags: row.Hashtags,
                Counters: new PostCountersDto(0, 0, 0
                    // counters.Reactions,
                    // counters.Comments,
                    // counters.Reposts
                    ),
                CreatedAt: row.CreatedAt,
                UpdatedAt: row.UpdatedAt));
        }

        _logger.LogInformation(
            "GetRepostsByPostId: {PostId} → {Count} reposts (hasMore: {HasMore})",
            request.PostId, items.Count, hasMore);

        return Success(new GetPostRepostsQueryResponse(
            Items: items,
            NextCursor: nextCursor,
            HasMore: hasMore));
    }
}