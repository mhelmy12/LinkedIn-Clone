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

namespace EngagementService.Features.Comments.EditComment;


public class EditCommentHandler(
    EngagmentDbContext _dbContext,
    [FromKeyedServices("Snowflake")] IIdGeneratorService _snowflakeIdGenerator,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
    ILogger<EditCommentHandler> _logger
) : ResponseHandler, IRequestHandler<EditCommentCommand, Response<EditCommentResponse>>
{
    public async Task<Response<EditCommentResponse>> Handle(
        EditCommentCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized<EditCommentResponse>("User is not authenticated.");



        var comment = await _dbContext.Comments
            .Include(c => c.Media)
            .Include(c => c.Mentions)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken);

        if (comment is null)
            return NotFound<EditCommentResponse>("Comment not found.");



        if (comment.AuthorId != currentUserId)
            return Unauthorized<EditCommentResponse>(
                "You can only edit your own comments.");

        var newContent = request.Content is null
            ? comment.Content
            : (string.IsNullOrWhiteSpace(request.Content) ? null : request.Content.Trim());

        var contentChanged = request.Content is not null
            && newContent != comment.Content;

        var mediaChanged = request.Media is not null;
        var mentionsChanged = request.Mentions is not null;

        if (!contentChanged && !mediaChanged && !mentionsChanged)
        {
            return Success(MapToResponse(comment, noChanges: true),
                "No changes were applied.");
        }

        var now = DateTime.UtcNow;
        var oldMentionIds = comment.Mentions
            .Select(m => m.MentionedUserId)
            .Distinct()
            .ToList();

   
        if (contentChanged)
        {
            comment.Content = newContent;
        }

        if (mediaChanged)
        {
            _dbContext.Set<CommentMedia>().RemoveRange(comment.Media);
            comment.Media.Clear();

            foreach (var m in request.Media!)
            {
                comment.Media.Add(new CommentMedia
                {
                    Id = long.Parse(_snowflakeIdGenerator.Generate()),
                    CommentId = comment.Id,
                    ObjectKey = m.ObjectKey,
                    MediaType = m.MediaType,
                    DisplayOrder = m.DisplayOrder,
                    CreatedAt = now
                });
            }
        }


        if (mentionsChanged)
        {
            _dbContext.Set<CommentMention>().RemoveRange(comment.Mentions);
            comment.Mentions.Clear();

            foreach (var m in request.Mentions!)
            {
                comment.Mentions.Add(new CommentMention
                {
                    Id = long.Parse(_snowflakeIdGenerator.Generate()),
                    CommentId = comment.Id,
                    MentionedUserId = m.MentionedUserId,
                    StartIndex = m.StartIndex,
                    Length = m.Length
                });
            }
        }

        var finalContent = comment.Content;
        var finalMediaCount = comment.Media.Count;

        if (string.IsNullOrWhiteSpace(finalContent) && finalMediaCount == 0)
            return BadRequest<EditCommentResponse>(
                "Comment must have content or media.");

        comment.UpdatedAt = now;



        var @event = new CommentUpdatedEvent(
            CommentId: comment.Id,
            PostId: comment.PostId,
            AuthorId: comment.AuthorId,
            ParentCommentId: comment.ParentCommentId,
            Content: comment.Content,
            ContentChanged: contentChanged,
            UpdatedAt: now);

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Payload = JsonSerializer.Serialize(@event),
            Type = nameof(CommentUpdatedEvent),
            AggregateType = nameof(Comment),
            AggregateId = comment.Id.ToString(),
            Timestamp = now
        });


        _logger.LogInformation($"Comment {comment.Id} updated by {currentUserId} (content: {contentChanged}, media: {mediaChanged}, mentions: {mentionsChanged})");

        return Success(MapToResponse(comment, noChanges: false),
            "Comment updated successfully.");
    }

    private static EditCommentResponse MapToResponse(Comment c, bool noChanges)
    {
        return new EditCommentResponse(
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
            UpdatedAt: c.UpdatedAt,
            NoChanges: noChanges);
    }
}
