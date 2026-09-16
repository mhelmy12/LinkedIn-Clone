using System;
using PostService.Features.CreatePost;
using PostService.Models;

namespace PostService.Events;

public record PostCreatedEvent(
    long PostId,
    string AuthorId,
    string? Content,
    VisibilityType Visibility,
    long? QuotedPostId,
    IReadOnlyList<string>? MentionedUserIds,
    IReadOnlyList<string>? Hashtags,
    DateTime CreatedAt);
// For Notification Service and ElasticSearch Service....