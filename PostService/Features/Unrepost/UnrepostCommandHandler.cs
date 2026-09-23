using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PostService.Data;
using PostService.Events;
using PostService.Models;
using PostService.Services.CurrentUserService;
using Shared.Helpers;

namespace PostService.Features.Unrepost;

public class UnrepostCommandHandler(
    PostDbContext _dbContext,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
    ILogger<UnrepostCommandHandler> _logger
) : ResponseHandler, IRequestHandler<UnrepostCommand, Response<UnrepostCommandResponse>>
{
    public async Task<Response<UnrepostCommandResponse>> Handle(
        UnrepostCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized<UnrepostCommandResponse>("User is not authenticated");

        // 1. Find the user's Pure Repost of this original post
        // conditions:
        //   - AuthorId = currentUser      → repost belongs to the current user
        //   - RepostOfPostId = originalId → repost of the specified post
        //   - Content == null             → Pure Repost (not Quote)
        //   - IsDeleted == false          → not already deleted
        var repost = await _dbContext.Posts
            .AsNoTracking()
            .Where(p => p.AuthorId == currentUserId)
            .Where(p => p.RepostOfPostId == request.OriginalPostId)
            .Where(p => p.Content == null)
            .Where(p => !p.IsDeleted)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (repost is null)
            return NotFound<UnrepostCommandResponse>("Repost not found.");

        var now = DateTime.Now;

        // 
        // 2. Soft delete (bulk UPDATE, atomic)
        // WHERE IsDeleted = false prevents race condition:
        //   - If another user deletes the post between SELECT and UPDATE,
        //   - The UPDATE will not affect any rows
        //   affected = 0 → 404
        var affected = await _dbContext.Posts
            .Where(p => p.Id == repost.Id && !p.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsDeleted, true),
                cancellationToken);

        if (affected == 0)
            return NotFound<UnrepostCommandResponse>("Repost not found.");

        // 3. Outbox: PostDeletedEvent
        // PostId = the repost's ID, not the original post's ID
        // This is because the FeedService and SearchService need to remove the repost,
        // not the original post.
        var @event = new PostDeletedEvent(
            PostId: repost.Id.ToString(),
            AuthorId: currentUserId,
            DeletedAt: now);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Payload = JsonSerializer.Serialize(@event),
            Type = nameof(PostDeletedEvent),
            AggregateType = nameof(Post),
            AggregateId = repost.Id.ToString(),
            Timestamp = now
        };

        _dbContext.OutboxMessages.Add(outboxMessage);

        _logger.LogInformation(
            "Unrepost: repost {RepostId} of original {OriginalId} removed by {AuthorId}",
            repost.Id, request.OriginalPostId, currentUserId);

        return Success(
            new UnrepostCommandResponse(),
            "Repost removed successfully.");
    }
}