using System;
using EngagementService.Data;
using EngagementService.Events;
using EngagementService.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using Shared.Services.CurrentUserService;

namespace EngagementService.Features.Comments.DeleteComment;

public class DeleteCommentHandler(
    EngagmentDbContext _dbContext,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
    ILogger<DeleteCommentHandler> _logger
) : ResponseHandler, IRequestHandler<DeleteCommentCommand, Response<DeleteCommentResponse>>
{
    public async Task<Response<DeleteCommentResponse>> Handle(
        DeleteCommentCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized<DeleteCommentResponse>("User is not authenticated.");

        var comment = await _dbContext.Comments
            .AsNoTracking()
            .Where(c => c.Id == request.CommentId)
            .Select(c => new
            {
                c.Id,
                c.PostId,
                c.AuthorId,
                c.ParentCommentId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (comment is null)
            return NotFound<DeleteCommentResponse>("Comment not found.");

        if (comment.AuthorId != currentUserId)
            return Unauthorized<DeleteCommentResponse>("You can only delete your own comments.");

        var now = DateTime.UtcNow;

        var deletedIds = await GetAllDescendantIdsAsync(
            comment.Id, cancellationToken);

        if (deletedIds.Count == 0)
        {
            _logger.LogWarning(
                "Comment {CommentId} has no descendants found — likely concurrent delete",
                comment.Id);
            return NotFound<DeleteCommentResponse>("Comment not found.");
        }

        var affected = await _dbContext.Comments
            .Where(c => deletedIds.Contains(c.Id) && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsDeleted, true)
                .SetProperty(c => c.UpdatedAt, now),
                cancellationToken);

        if (affected == 0)
        {
            _logger.LogWarning(
                "No rows affected for comment {CommentId} — likely concurrent delete",
                comment.Id);
            return NotFound<DeleteCommentResponse>("Comment not found.");
        }

        if (comment.ParentCommentId.HasValue)
        {
            var ancestorIds = await GetAncestorIdsAsync(
                comment.ParentCommentId.Value, cancellationToken);

            if (ancestorIds.Count > 0)
            {
                await _dbContext.Comments
                    .Where(c => ancestorIds.Contains(c.Id))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.RepliesCount,
                                     c => c.RepliesCount - affected),
                        cancellationToken);
            }
        }

        var @event = new CommentDeletedEvent(
            CommentId: comment.Id,
            PostId: comment.PostId,
            AuthorId: comment.AuthorId,
            ParentCommentId: comment.ParentCommentId,
            DeletedCommentIds: deletedIds,
            DeletedAt: now);

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Payload = System.Text.Json.JsonSerializer.Serialize(@event),
            Type = nameof(CommentDeletedEvent),
            AggregateType = nameof(Comment),
            AggregateId = comment.Id.ToString(),
            Timestamp = now
        });

        _logger.LogInformation(
            "Comment {CommentId} deleted by {AuthorId} (deleted count: {Count}, post: {PostId})",
            comment.Id, currentUserId, affected, comment.PostId);

        return Success<DeleteCommentResponse>(null, "Comment deleted successfully.");
    }
    private async Task<List<long>> GetAllDescendantIdsAsync(
        long commentId,
        CancellationToken ct)
    {
        var ids = await _dbContext.Database
            .SqlQuery<long>($@"
                WITH RECURSIVE descendants AS (
                    SELECT ""Id"", ""ParentCommentId""
                    FROM ""Comments""
                    WHERE ""Id"" = {commentId} AND ""IsDeleted"" = false
                    
                    UNION ALL
                    
                    SELECT c.""Id"", c.""ParentCommentId""
                    FROM ""Comments"" c
                    INNER JOIN descendants d ON c.""ParentCommentId"" = d.""Id""
                    WHERE c.""IsDeleted"" = false
                )
                SELECT ""Id"" FROM descendants")
            .ToListAsync(ct);

        return ids;
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
}
