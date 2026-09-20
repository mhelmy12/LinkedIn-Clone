using System;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PostService.Data;
using PostService.Events;
using PostService.Helpers;
using PostService.Models;
using PostService.Services.CurrentUserService;
using PostService.Services.UserIdGenerator;
using Shared.Helpers;

namespace PostService.Features.EditPost;

public class EditPostCommandHandler(
    PostDbContext _db,
    [FromKeyedServices("Headers")] ICurrentUserService currentUserService,
    IConfiguration options,
    ILogger<EditPostCommandHandler> logger,
    [FromKeyedServices("Snowflake")] IUserIdGenerator snowflakeIdGenerator

) : ResponseHandler, IRequestHandler<EditPostCommand, Response<EditPostCommandResponse>>

{

    public async Task<Response<EditPostCommandResponse>> Handle(EditPostCommand request, CancellationToken ct)
    {

        var currentUserId = currentUserService.GetCurrentUserId();
        var editWindow = options.GetSection("PostOptions")["EditWindowMinutes"] is string minutesStr
            && int.TryParse(minutesStr, out var minutes)
            ? minutes
            : 60; // default to 60 minutes if not configured


        // ─── 1. Load the Post with all children (tracked) ───
        var post = await _db.Posts
            .Include(p => p.Media)
            .Include(p => p.Mentions)
            .Include(p => p.PostHashtags)
            .FirstOrDefaultAsync(p => p.Id == long.Parse(request.PostId), ct);

        // ─── 2. Not found (includes soft-deleted via QueryFilter) ───
        if (post is null)
            return NotFound<EditPostCommandResponse>("Post not found.");

        // ─── 3. Ownership check ───
        if (post.AuthorId != currentUserId)
            return Unauthorized<EditPostCommandResponse>("You can only edit your own posts.");

        // ─── 4. Time window check ───
        var age = DateTimeOffset.UtcNow - post.CreatedAt;
        var window = TimeSpan.FromMinutes(editWindow);

        if (age > window)
            return Unproccess<EditPostCommandResponse>(
                $"Post can only be edited within {editWindow} minutes of creation.");

        // ─── 5. RowVersion format check (safety net) ───
        byte[] clientRowVersion;
        try
        {
            clientRowVersion = Convert.FromBase64String(request.RowVersion);
        }
        catch (FormatException)
        {
            return Unproccess<EditPostCommandResponse>(
                "Invalid RowVersion format.");
        }

        // ─── 6. Detect what changed ───
        var contentChanged = request.Content is not null
            && request.Content.Trim() != post.Content;

        var visibilityChanged = request.Visibility is not null
            && request.Visibility.Value != post.Visibility;

        var mediaChanged = request.Media is not null;

        var mentionsChanged = request.Mentions is not null;

        // ─── 7. If nothing actually changed → early return ───
        if (!contentChanged && !visibilityChanged
            && !mediaChanged && !mentionsChanged)
        {
            return Success<EditPostCommandResponse>(new EditPostCommandResponse(
                PostId: post.Id.ToString(),
                UpdatedAt: post.UpdatedAt ?? post.CreatedAt,
                RowVersion: Convert.ToBase64String(post.RowVersion!),
                NoChanges: true));
        }

        var now = DateTime.Now;

        // ─── 8. Apply Content change ───
        var newContent = post.Content;
        if (contentChanged)
        {
            newContent = string.IsNullOrWhiteSpace(request.Content)
                ? null
                : request.Content!.Trim();

            post.Content = newContent;
        }

        // ─── 9. Apply Visibility change ───
        if (visibilityChanged)
        {
            post.Visibility = request.Visibility!.Value;
        }

        // ─── 10. Apply Media replacement ───
        if (mediaChanged)
        {
            _db.PostMedia.RemoveRange(post.Media);

            foreach (var m in request.Media!)
            {
                post.Media.Add(new PostMedia
                {
                    Id = long.Parse(snowflakeIdGenerator.Generate()),
                    PostId = post.Id,
                    MediaKey = m.MediaKey,
                    MediaType = m.MediaType,
                    DisplayOrder = m.DisplayOrder,

                });
            }
        }

        // ─── 11. Apply Mentions replacement ───
        var oldMentionIds = post.Mentions
            .Select(m => m.MentionedEntityId)
            .ToList();

        var newMentionIds = oldMentionIds;

        if (mentionsChanged)
        {
            _db.PostMentions.RemoveRange(post.Mentions);

            foreach (var m in request.Mentions!)
            {
                post.Mentions.Add(new PostMention
                {
                    Id = long.Parse(snowflakeIdGenerator.Generate()),
                    PostId = post.Id,
                    MentionedEntityId = m.MentionedUserId,
                    StartIndex = m.StartIndex,
                    Length = m.Length
                });
            }

            newMentionIds = request.Mentions!
                .Select(m => m.MentionedUserId)
                .Distinct()
                .ToList();
        }

        // ─── 12. Apply Hashtags (only when Content changed) ───
        var newTags = new List<string>();
        if (contentChanged)
        {
            newTags = string.IsNullOrWhiteSpace(newContent)
                ? new List<string>()
                : HashtagExtractor.Extract(newContent);

            _db.PostHashtags.RemoveRange(post.PostHashtags);

            foreach (var tag in newTags)
            {
                post.PostHashtags.Add(new PostHashtag
                {
                    PostId = post.Id,
                    NormalizedName = tag
                });
            }
        }
        else
        {
            newTags = post.PostHashtags
                .Select(ph => ph.NormalizedName)
                .ToList();
        }

        // ─── 13. Final consistency check ───
        var finalContent = post.Content;
        var finalMediaCount = post.Media.Count;

        if (string.IsNullOrWhiteSpace(finalContent) && finalMediaCount == 0)
        {
            return Unproccess<EditPostCommandResponse>(
                "Post must have content or at least one media item.");
        }

        // ─── 14. Update timestamp + set original RowVersion for concurrency ───
        post.UpdatedAt = now;

        // EF uses the original RowVersion loaded from DB
        // to add WHERE RowVersion = @original in the UPDATE.
        // We just need to make sure the client's RowVersion matches.
        if (!post.RowVersion!.SequenceEqual(clientRowVersion))
        {
            return Unproccess<EditPostCommandResponse>(
                "Post was modified by another request. Please refresh and try again.");
        }

        // ─── 15. Write the Outbox event ───
        var @event = new PostUpdatedEvent(
            PostId: post.Id.ToString(),
            AuthorId: post.AuthorId,
            Content: post.Content,
            Visibility: (int)post.Visibility,
            Hashtags: newTags.ToList(),
            NewMentionedUserIds: newMentionIds,
            OldMentionedUserIds: oldMentionIds,
            ContentChanged: contentChanged,
            VisibilityChanged: visibilityChanged,
            MediaChanged: mediaChanged,
            UpdatedAt: now);

        var postUpdatedEventJson = JsonSerializer.Serialize(@event);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Payload = postUpdatedEventJson,
            Type = nameof(PostUpdatedEvent),
            AggregateType = nameof(Post),
            AggregateId = post.Id.ToString(),
            Timestamp = now
        };

        _db.OutboxMessages.Add(outboxMessage);



        logger.LogInformation(
            "Post {PostId} updated by {AuthorId}",
            post.Id, post.AuthorId);

        // ─── 17. Return ───
        return Success<EditPostCommandResponse>(new EditPostCommandResponse(
            PostId: post.Id.ToString(),
            UpdatedAt: now,
            RowVersion: Convert.ToBase64String(post.RowVersion!),
            NoChanges: false));
    }

}

