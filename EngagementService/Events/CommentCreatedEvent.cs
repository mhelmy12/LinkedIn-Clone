using System;

namespace EngagementService.Events;

public record CommentCreatedEvent(
    long CommentId,
    long PostId,
    string AuthorId,
    long? ParentCommentId,
    string? Content,
    List<string> MentionedUserIds,
    DateTime CreatedAt);