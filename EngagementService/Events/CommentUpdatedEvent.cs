using System;

namespace EngagementService.Events;

public record CommentUpdatedEvent(
    long CommentId,
    long PostId,
    string AuthorId,
    long? ParentCommentId,
    string? Content,
    bool ContentChanged,
    DateTime UpdatedAt);