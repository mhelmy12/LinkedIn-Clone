using System;
using System.Text.Json;
using EngagementService.Data;
using EngagementService.Events;
using EngagementService.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using Shared.Services.CurrentUserService;
using Shared.Services.IdGeneratorService;

namespace EngagementService.Features.Comments.CreateComment;


public class CreateCommentHandler(
    EngagmentDbContext _dbContext,
    [FromKeyedServices("Snowflake")] IIdGeneratorService _snowflakeIdGenerator,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
    ILogger<CreateCommentHandler> _logger
) : ResponseHandler, IRequestHandler<CreateCommentCommand, Response<CreateCommentResponse>>
{
    private const int MaxDepth = 10;

    public async Task<Response<CreateCommentResponse>> Handle(
        CreateCommentCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized<CreateCommentResponse>("User is not authenticated.");

        int depth = 0;

        if (request.ParentCommentId.HasValue)
        {
            var parent = await _dbContext.Comments
                .AsNoTracking()
                .Where(c => c.Id == request.ParentCommentId.Value)
                .Select(c => new { c.Id, c.PostId, c.Depth })
                .FirstOrDefaultAsync(cancellationToken);

            if (parent is null)
                return NotFound<CreateCommentResponse>("Parent comment not found.");

            if (parent.Depth >= MaxDepth)
                return BadRequest<CreateCommentResponse>(
                    $"Maximum reply depth ({MaxDepth}) reached.");

            if (parent.PostId != request.PostId)
                return BadRequest<CreateCommentResponse>(
                    "Parent comment does not belong to this post.");

            depth = parent.Depth + 1;
        }

        var now = DateTime.UtcNow;
        var commentId = long.Parse(_snowflakeIdGenerator.Generate());

        var comment = new Comment
        {
            Id = commentId,
            PostId = request.PostId,
            AuthorId = currentUserId,
            ParentCommentId = request.ParentCommentId,
            Depth = depth,
            RepliesCount = 0,
            Content = string.IsNullOrWhiteSpace(request.Content)
                ? null
                : request.Content.Trim(),
            CreatedAt = now,
            IsDeleted = false
        };


        if (request.Media is { Count: > 0 })
        {
            foreach (var m in request.Media)
            {
                comment.Media.Add(new CommentMedia
                {
                    Id = long.Parse(_snowflakeIdGenerator.Generate()),
                    CommentId = commentId,
                    ObjectKey = m.ObjectKey,
                    MediaType = m.MediaType,
                    DisplayOrder = m.DisplayOrder,
                    CreatedAt = now
                });
            }
        }

        if (request.Mentions is { Count: > 0 })
        {
            foreach (var m in request.Mentions)
            {
                comment.Mentions.Add(new CommentMention
                {
                    Id = long.Parse(_snowflakeIdGenerator.Generate()),
                    CommentId = commentId,
                    MentionedUserId = m.MentionedUserId,
                    StartIndex = m.StartIndex,
                    Length = m.Length
                });
            }
        }

        _dbContext.Comments.Add(comment);

        if (request.ParentCommentId.HasValue)
        {
            var ancestorIds = await GetAncestorIdsAsync(
                request.ParentCommentId.Value, cancellationToken);

            await _dbContext.Comments
                .Where(c => ancestorIds.Contains(c.Id))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.RepliesCount, c => c.RepliesCount + 1),
                    cancellationToken);
        }

        var @event = new CommentCreatedEvent(
            CommentId: commentId,
            PostId: comment.PostId,
            AuthorId: comment.AuthorId,
            ParentCommentId: comment.ParentCommentId,
            Content: comment.Content,
            MentionedUserIds: request.Mentions?
                .Select(m => m.MentionedUserId)
                .Distinct()
                .ToList() ?? new List<string>(),
            CreatedAt: now);


        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Payload = JsonSerializer.Serialize(@event),
            Type = nameof(CommentCreatedEvent),
            AggregateType = nameof(Comment),
            AggregateId = commentId.ToString(),
            Timestamp = now
        });



        _logger.LogInformation(
            "Comment {CommentId} created by {AuthorId} on Post {PostId} (depth: {Depth}, parent: {Parent})",
            commentId, currentUserId, request.PostId, depth, request.ParentCommentId);

        return Success(MapToResponse(comment), "Comment created successfully.");
    }

    private async Task<List<long>> GetAncestorIdsAsync(
        long commentId,
        CancellationToken ct)
    {
        var ids = await _dbContext.Database
            .SqlQuery<long>($@"
                WITH RECURSIVE ancestors AS (
                    SELECT ""Id"", ""ParentCommentId""
                    FROM ""Comments""
                    WHERE ""Id"" = {commentId}
                    
                    UNION ALL
                    
                    SELECT c.""Id"", c.""ParentCommentId""
                    FROM ""Comments"" c
                    INNER JOIN ancestors a ON c.""Id"" = a.""ParentCommentId""
                )
                SELECT ""Id"" FROM ancestors")
            .ToListAsync(ct);

        return ids;
    }
    private static CreateCommentResponse MapToResponse(Comment c)
    {
        return new CreateCommentResponse(
            CommentId: c.Id,
            PostId: c.PostId,
            ParentCommentId: c.ParentCommentId,
            Depth: c.Depth,
            AuthorId: c.AuthorId,
            Content: c.Content,
            Media: c.Media
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new MediaDto(m.ObjectKey, m.MediaType, m.DisplayOrder))
                .ToList(),
            Mentions: c.Mentions
                .OrderBy(m => m.StartIndex)
                .Select(m => new MentionDto(m.MentionedUserId, m.StartIndex, m.Length))
                .ToList(),
            CreatedAt: c.CreatedAt,
            UpdatedAt: c.UpdatedAt);
    }
}
