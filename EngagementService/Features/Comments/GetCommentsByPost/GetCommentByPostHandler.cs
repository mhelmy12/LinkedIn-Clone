using System;
using EngagementService.Data;
using EngagementService.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using Shared.Services.CurrentUserService;

namespace EngagementService.Features.Comments.GetCommentsByPost;


public class GetCommentsByPostHandler(
    EngagmentDbContext _dbContext,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
    ILogger<GetCommentsByPostHandler> _logger
) : ResponseHandler, IRequestHandler<GetCommentsByPostQuery, Response<GetCommentsByPostResponse>>
{
    public async Task<Response<GetCommentsByPostResponse>> Handle(
        GetCommentsByPostQuery request,
        CancellationToken cancellationToken)
    {

        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized<GetCommentsByPostResponse>("User is not authenticated.");

        DateTime? cursorCreatedAt = null;
        long? cursorId = null;

        if (!string.IsNullOrEmpty(request.Cursor))
        {
            if (!CursorCodec.TryDecode(request.Cursor, out var dt, out var id))
                return BadRequest<GetCommentsByPostResponse>("Invalid cursor.");

            cursorCreatedAt = dt;
            cursorId = id;
        }


        var query = _dbContext.Comments
            .AsNoTracking()
            .Where(c => c.PostId == request.PostId)
            .Where(c => c.ParentCommentId == null);

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            query = query.Where(c =>
                c.CreatedAt > cursorCreatedAt.Value
                || (c.CreatedAt == cursorCreatedAt.Value
                    && c.Id > cursorId.Value));
        }

        var rows = await query
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Take(request.Limit + 1)
            .Select(c => new
            {
                c.Id,
                c.PostId,
                c.ParentCommentId,
                c.Depth,
                c.AuthorId,
                c.Content,
                c.RepliesCount,
                c.CreatedAt,
                c.UpdatedAt,
                Media = c.Media
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => new MediaDto(
                        m.ObjectKey,
                        m.MediaType,
                        m.DisplayOrder))
                    .ToList(),
                Mentions = c.Mentions
                    .OrderBy(m => m.StartIndex)
                    .Select(m => new MentionDto(
                        m.MentionedUserId,
                        m.StartIndex,
                        m.Length))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > request.Limit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        string? nextCursor = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextCursor = CursorCodec.Encode(last.CreatedAt, last.Id.ToString());
        }

        var items = rows
            .Select(c => new CommentItemDto(
                CommentId: c.Id,
                PostId: c.PostId,
                ParentCommentId: c.ParentCommentId,
                Depth: c.Depth,
                AuthorId: c.AuthorId.ToString(),
                Content: c.Content,
                Media: c.Media,
                Mentions: c.Mentions,
                RepliesCount: c.RepliesCount,
                CreatedAt: c.CreatedAt,
                UpdatedAt: c.UpdatedAt))
            .ToList();

        _logger.LogInformation($"GetCommentsByPost: Post {request.PostId} → {items.Count} comments (hasMore: {hasMore})");

        return Success(new GetCommentsByPostResponse(
            Items: items,
            NextCursor: nextCursor,
            HasMore: hasMore));
    }
}
