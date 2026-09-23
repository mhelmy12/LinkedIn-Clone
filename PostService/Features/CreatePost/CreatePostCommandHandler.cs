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

namespace PostService.Features.CreatePost;

public class CreatePostCommandHandler(
    PostDbContext _dbContext,
    [FromKeyedServices("Snowflake")] IUserIdGenerator _snowflakeIdGenerator,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
    ILogger<CreatePostCommandHandler> logger
) : ResponseHandler, IRequestHandler<CreatePostCommand, Response<CreatePostCommandResponse>>
{
    public async Task<Response<CreatePostCommandResponse>> Handle(
        CreatePostCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized<CreatePostCommandResponse>("User is not authenticated");

        // 1. Classify the post
        var hasContent = !string.IsNullOrWhiteSpace(request.Content);
        var hasMedia = request.Media is { Count: > 0 };
        var isRepost = request.RepostOfPostId.HasValue;

        // 2. Validation of the combination
        // - Original: must have content OR media
        // - Quote:    must have content, no media
        // - Pure:     no content, no media

        if (isRepost && hasMedia)
            return BadRequest<CreatePostCommandResponse>("Reposts cannot have media.");

        if (!isRepost && !hasContent && !hasMedia)
            return BadRequest<CreatePostCommandResponse>("Post must have content or media.");

        // 3. Resolve the original post + flatten chains
        long? canonicalOriginalId = null;

        if (isRepost)
        {
            var original = await _dbContext.Posts
                .AsNoTracking()
                .Where(p => p.Id == request.RepostOfPostId!.Value && !p.IsDeleted)
                .Select(p => new { p.Id, p.RepostOfPostId })
                .FirstOrDefaultAsync(cancellationToken);

            if (original is null)
                return NotFound<CreatePostCommandResponse>("Original post not found.");

            // Flatten: if the target is itself a repost, point to its true original.
            // This prevents "repost of a repost" chains from ever being created.
            canonicalOriginalId = original.RepostOfPostId ?? original.Id;
        }

        // 4. Uniqueness check for Pure Reposts
        if (isRepost && !hasContent)
        {
            var alreadyReposted = await _dbContext.Posts
                .AnyAsync(p =>
                    p.AuthorId == currentUserId
                    && p.RepostOfPostId == canonicalOriginalId
                    && p.Content == null
                    && !p.IsDeleted,
                    cancellationToken);

            if (alreadyReposted)
                return BadRequest<CreatePostCommandResponse>(
                    "You have already reposted this post.");
        }

        // ═══════════════════════════════════════════════════════════
        // 5. Build the post
        // ═══════════════════════════════════════════════════════════
        var postId = long.Parse(_snowflakeIdGenerator.Generate());
        var now = DateTime.UtcNow;

        var postType = isRepost
            ? (hasContent ? PostType.Quote : PostType.PureRepost)
            : PostType.Original;

        var post = new Post
        {
            Id = postId,
            AuthorId = currentUserId,
            Content = hasContent ? request.Content!.Trim() : null,
            Visibility = request.Visibility,
            RepostOfPostId = canonicalOriginalId,   // canonical, not the direct target
            CreatedAt = now,
            Type = postType
        };

        // ═══════════════════════════════════════════════════════════
        // 6. Mentions
        // ═══════════════════════════════════════════════════════════
        if (request.Mentions is { Count: > 0 })
        {
            foreach (var m in request.Mentions)
            {
                post.Mentions.Add(new PostMention
                {
                    Id = long.Parse(_snowflakeIdGenerator.Generate()),
                    PostId = postId,
                    MentionedEntityId = m.MentionedUserId,
                    StartIndex = m.StartIndex,
                    Length = m.Length
                });
            }
        }

        // 7. Hashtags (extracted from content only)
        var normalizedHashtags = hasContent
            ? HashtagExtractor.Extract(request.Content!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
            : new List<string>();

        foreach (var tag in normalizedHashtags)
        {
            post.PostHashtags.Add(new PostHashtag
            {
                PostId = postId,
                NormalizedName = tag
            });
        }

        // 8. Persist
        _dbContext.Posts.Add(post);

        // 9. Outbox event
        var postCreatedEvent = new PostCreatedEvent(
            PostId: postId,
            AuthorId: currentUserId,
            Content: post.Content,
            Visibility: post.Visibility,
            RepostOfPostId: canonicalOriginalId,
            MentionedUserIds: request.Mentions?
                .Select(m => m.MentionedUserId)
                .Distinct()
                .ToList() ?? new List<string>(),
            Hashtags: normalizedHashtags,
            CreatedAt: now);

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Payload = JsonSerializer.Serialize(postCreatedEvent),
            Type = nameof(PostCreatedEvent),
            AggregateType = nameof(Post),
            AggregateId = postId.ToString(),
            Timestamp = now
        });

        logger.LogInformation(
            "Post {PostId} created by {AuthorId} (type: {Type}, repostOf: {RepostOf})",
            postId, currentUserId, postType, canonicalOriginalId);

        return Success(
            new CreatePostCommandResponse(postId),
            "Post created successfully");
    }
}