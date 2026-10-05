namespace FeedService.Helpers;

public record PostCounters(
    long PostId,
    long Reactions,
    long Comments,
    long Reposts)
{
    public static PostCounters Empty(long postId) =>
        new(postId, 0, 0, 0);
}
