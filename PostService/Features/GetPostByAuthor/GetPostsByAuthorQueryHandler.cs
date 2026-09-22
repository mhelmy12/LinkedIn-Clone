using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PostService.Data;
using PostService.Helpers;
using PostService.Models;
using PostService.Services.CurrentUserService;
using Shared.Helpers;

namespace PostService.Features.GetPostByAuthor;



public class GetPostsByAuthorQueryHandler(
        PostDbContext _db,
        [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
        // IPostCountersReader _counters,
        ILogger<GetPostsByAuthorQueryHandler> _logger
) : ResponseHandler, IRequestHandler<GetPostsByAuthorQuery, Response<GetPostsByAuthorQueryResponse>>
{
    public async Task<Response<GetPostsByAuthorQueryResponse>> Handle(
        GetPostsByAuthorQuery request,
        CancellationToken ct)
    {
        var currentUser = _currentUserService.GetCurrentUserId();

        // 1. Parse cursor
        DateTime? cursorCreatedAt = null;
        long? cursorId = null;

        if (!string.IsNullOrEmpty(request.Cursor))
        {
            if (!CursorCodec.TryDecode(request.Cursor, out var dt, out var id))
                return BadRequest<GetPostsByAuthorQueryResponse>("Invalid cursor.");

            cursorCreatedAt = dt;
            cursorId = id;
        }

        // 2. Main Query
        var query = _db.Posts
            .AsNoTracking()
            .Where(p => p.AuthorId == request.AuthorId)
            .Where(p => !p.IsDeleted);

        // visibility filter (in SQL)
        if (currentUser != request.AuthorId)
        {
            query = query.Where(p =>
                p.Visibility != VisibilityType.Private);
        }

        // keyset cursor
        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            query = query.Where(p =>
                p.CreatedAt < cursorCreatedAt.Value
                || (p.CreatedAt == cursorCreatedAt.Value
                    && p.Id < cursorId.Value));
        }

        // Limit + 1 -> HasMore??
        var rows = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(request.Limit + 1)
            .Select(p => new
            {
                p.Id,
                p.AuthorId,
                p.Content,
                p.Visibility,
                p.RepostOfPostId,
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

        // 3. HasMore + NextCursor
        bool hasMore = rows.Count > request.Limit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);  // remove the extra row

        string? nextCursor = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextCursor = CursorCodec.Encode(last.CreatedAt, last.Id.ToString());
        }

        // 4. Empty → 200 + empty list
        if (rows.Count == 0)
        {
            return Success(new GetPostsByAuthorQueryResponse(
                Items: new List<PostSummaryDto>(),
                NextCursor: null,
                HasMore: false));
        }

        // 5. Batch Load RepostOf (instead N+1)
        var repostOfIds = rows
            .Where(p => p.RepostOfPostId.HasValue)
            .Select(p => p.RepostOfPostId!.Value)
            .Distinct()
            .ToList();

        var repostOfsMap = new Dictionary<long, RepostOfSummaryDto>();
        if (repostOfIds.Count > 0)
        {
            var repostOfs = await _db.Posts
                .AsNoTracking()
                .Where(p => repostOfIds.Contains(p.Id))
                .Where(p => !p.IsDeleted)
                .Where(p =>
                    p.Visibility != VisibilityType.Private
                    || p.AuthorId == currentUser)
                .Select(p => new
                {
                    p.Id,
                    p.AuthorId,
                    p.Content,
                    p.CreatedAt,

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

            foreach (var r in repostOfs)
            {
                repostOfsMap[r.Id] = new RepostOfSummaryDto(
                    Id: r.Id.ToString(),
                    AuthorId: r.AuthorId,
                    // Author: r.Author,
                    Author: null,
                    Content: r.Content,
                    Media: r.Media,
                    Mentions: r.Mentions
                        .Select(m => new MentionDto(
                            // DisplayName: m.User?.DisplayName
                            //     ?? $"User #{m.MentionedEntityId}",
                            // ProfileImageKey: m.User?.ProfileImageKey,
                            UserId: m.MentionedEntityId,
                            DisplayName: "",
                            ProfileImageKey: "",
                            StartIndex: m.StartIndex,
                            Length: m.Length))
                        .ToList(),
                    Hashtags: r.Hashtags,
                    CreatedAt: r.CreatedAt);
            }
        }

        // // 6. Collect canonical IDs + RepostOf IDs → Redis MGET
        //TODO: Implement redis counters in the future

        // var counterKeys = new HashSet<long>();

        // foreach (var row in rows)
        // {
        //     var isPureRepost = row.Content is null && row.RepostOfPostId.HasValue;

        //     var canonicalId = isPureRepost
        //         ? row.RepostOfPostId!.Value
        //         : row.Id;

        //     counterKeys.Add(canonicalId);

        //     if (row.RepostOfPostId.HasValue)
        //         counterKeys.Add(row.RepostOfPostId.Value);
        // }

        // var countersMap = await _counters.GetManyAsync(counterKeys, ct);

        // 7. Build PostSummaryDto list
        var items = new List<PostSummaryDto>(rows.Count);

        foreach (var row in rows)
        {
            var isPureRepost = row.Content is null && row.RepostOfPostId.HasValue;

            var canonicalId = isPureRepost
                ? row.RepostOfPostId!.Value
                : row.Id;

            RepostOfSummaryDto? repostOf = null;
            bool isRepostOfUnavailable = false;

            if (row.RepostOfPostId.HasValue)
            {
                repostOfsMap.TryGetValue(row.RepostOfPostId.Value, out repostOf);
                isRepostOfUnavailable = repostOf is null;
            }

            // ⭐ Pure Repost + Unavailable + not current user → skip
            if (isPureRepost && repostOf is null && row.AuthorId != currentUser)
            {
                continue;
            }

            // countersMap.TryGetValue(canonicalId, out var counters);

            items.Add(new PostSummaryDto(
                Id: row.Id.ToString(),
                CanonicalPostId: canonicalId.ToString(),
                AuthorId: row.AuthorId,
                Author: null,
                // Author: row.Author,
                Content: row.Content,
                Visibility: (int)row.Visibility,
                IsPureRepost: isPureRepost,
                RepostOfPostId: row.RepostOfPostId.ToString(),
                RepostOf: repostOf,
                IsRepostOfUnavailable: isRepostOfUnavailable,
                Media: row.Media,
                Mentions: row.Mentions
                    .Select(m => new MentionDto(
                        // DisplayName: m.User?.DisplayName
                        //     ?? $"User #{m.MentionedUserId}",
                        // ProfileImageKey: m.User?.ProfileImageKey,
                        UserId: m.MentionedEntityId,
                        DisplayName: "",
                        ProfileImageKey: "",
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
            "GetPostsByAuthorId: {AuthorId} → {Count} posts (hasMore: {HasMore})",
            request.AuthorId, items.Count, hasMore);

        return Success(new GetPostsByAuthorQueryResponse(
            Items: items,
            NextCursor: nextCursor,
            HasMore: hasMore));
    }
}