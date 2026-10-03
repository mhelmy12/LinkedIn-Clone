using System;

namespace EngagementService.Events;

public record CommentDeletedEvent(
    long CommentId,           // root
    long PostId,
    string AuthorId,
    long? ParentCommentId,
    List<long> DeletedCommentIds,   // all deleted (root + descendants)
    DateTime DeletedAt);