using System;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PostService.Data;
using PostService.Events;
using PostService.Models;
using PostService.Services.CurrentUserService;
using Shared.Helpers;

namespace PostService.Features.DeletePost;

public class DeletePostCommandHandler(
        PostDbContext _db,
        [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
        ILogger<DeletePostCommandHandler> _logger

) : ResponseHandler, IRequestHandler<DeletePostCommand, Response<DeletePostCommandResponse>>
{
    public async Task<Response<DeletePostCommandResponse>> Handle(DeletePostCommand request, CancellationToken ct)
    {


        var currentUser = _currentUserService.GetCurrentUserId();
        var postId = long.Parse(request.PostId);


        var post = await _db.Posts
            .AsNoTracking()
            .Where(p => p.Id == postId && p.IsDeleted == false)
            .Select(p => new
            {
                p.Id,
                p.AuthorId
            })
            .FirstOrDefaultAsync(ct);

        // ─── Deleted or not found ───
        if (post is null)
            return NotFound<DeletePostCommandResponse>("Post not found.");

        // ─── Not the owner of the post ───
        if (post.AuthorId != currentUser)
            return Unauthorized<DeletePostCommandResponse>("You can only delete your own posts.");

        var now = DateTime.Now;

        // ─── 5. Soft delete for Post (bulk UPDATE) ───
        var affected = await _db.Posts
            .Where(p => p.Id == postId && p.IsDeleted == false)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsDeleted, true),
                ct);

        // ─── if no rows were affected, the post was not found ───
        if (affected == 0)
            return NotFound<DeletePostCommandResponse>("Post not found.");


        // ─── 6. Outbox Event ───
        var @event = new PostDeletedEvent(
            PostId: postId.ToString(),
            AuthorId: post.AuthorId,
            DeletedAt: now);

        var postDeletedEventJson = JsonSerializer.Serialize(@event);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Payload = postDeletedEventJson,
            Type = nameof(PostDeletedEvent),
            AggregateType = nameof(Post),
            AggregateId = post.Id.ToString(),
            Timestamp = now
        };

        _db.OutboxMessages.Add(outboxMessage);

        _logger.LogInformation(
            "Post {PostId} deleted by {AuthorId}",
            post.Id, post.AuthorId
            );

        return Success<DeletePostCommandResponse>(new(), "Post deleted successfully.");
    }
}
