using System;

namespace FeedService.Kafka.Consumers;

public class CommentDeletedConsumer
{

}
public record CommentDeletedEvent(
    long CommentId,           // root
    long PostId,
    string AuthorId,
    long? ParentCommentId,
    List<long> DeletedCommentIds,   // all deleted (root + descendants)
    DateTime DeletedAt);
