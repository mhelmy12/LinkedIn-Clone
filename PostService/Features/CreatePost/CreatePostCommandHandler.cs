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

namespace PostService.Features.CreatePost;

public class CreatePostCommandHandler(
    PostDbContext _dbContext,
    [FromKeyedServices("Snowflake")] IUserIdGenerator _snowflakeIdGenerator,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
     ILogger<CreatePostCommandHandler> logger

) : ResponseHandler, IRequestHandler<CreatePostCommand, Response<CreatePostCommandResponse>>
{
    public async Task<Response<CreatePostCommandResponse>> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId(); //keycloakId
        if (currentUserId == null)
        {
            return Unauthorized<CreatePostCommandResponse>("User is not authenticated");
        }

        var postId = long.Parse(_snowflakeIdGenerator.Generate());
        var now = DateTime.Now;

        if (request.QuotedPostId.HasValue)
        {
            var originalExists = await _dbContext.Posts
                .AnyAsync(p => p.Id == request.QuotedPostId.Value, cancellationToken);

            if (!originalExists)
                return NotFound<CreatePostCommandResponse>("Quoted post not found.");
        }




        var post = new Post
        {
            Id = postId,
            AuthorId = currentUserId,
            Content = request.Content?.Trim(),
            Visibility = request.Visibility,
            QuotedPostId = request.QuotedPostId,
            CreatedAt = now,
            Type = request.QuotedPostId.HasValue ? PostType.Quote : PostType.Original,
        };

        if (request.Mentions != null)
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



        // 5. Hashtags 

        var normalizedHashtags = string.IsNullOrWhiteSpace(request.Content)
            ? new List<string>()
            : HashtagExtractor.Extract(request.Content);

        foreach (var tag in normalizedHashtags)
        {
            post.PostHashtags.Add(new PostHashtag
            {
                PostId = postId,
                NormalizedName = tag
            });
        }


        _dbContext.Posts.Add(post);

        var postCreatedEvent = new PostCreatedEvent(
          PostId: postId,
          AuthorId: post.AuthorId,
          Content: post.Content,
          Visibility: post.Visibility,
          QuotedPostId: post.QuotedPostId,
          MentionedUserIds: request.Mentions is null ? null : request.Mentions.Select(m => m.MentionedUserId).ToList(),
          Hashtags: normalizedHashtags,
          CreatedAt: now);



        var postCreatedEventJson = JsonSerializer.Serialize(postCreatedEvent);
        logger.LogInformation("PostCreatedEvent: {PostCreatedEventJson}", postCreatedEventJson
        );

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Payload = postCreatedEventJson,
            Type = nameof(PostCreatedEvent),
            AggregateType = nameof(Post),
            AggregateId = postId.ToString(),
            Timestamp = now
        };

        _dbContext.OutboxMessages.Add(outboxMessage);


        logger.LogInformation(
           "Post {PostId} created by {AuthorId}", postId, post.AuthorId);



        return Success(new CreatePostCommandResponse(postId), "Post created successfully");
    }
}
