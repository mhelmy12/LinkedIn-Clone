namespace FeedService.Helpers.RedisConfigurations;

public static class RedisKeys
{
    // Feed entries: Sorted SetS
    //   ZADD feed:{userId} score postId
    public static string Feed(string userId) => $"feed:{userId}";

    // Post counters: Hash
    //   HINCRBY post:{postId}:counters reactions 1
    public static string PostCounters(long postId) => $"post:{postId}:counters";

    // Post snapshot cache: Hash
    //   HSET post:{postId}:snapshot field value
    public static string PostSnapshot(long postId) => $"post:{postId}:snapshot";

    // Connections: Set
    //   SADD connections:{userId} connectedUserId
    public static string Connections(string userId) => $"connections:{userId}";

    // Hash field names (constants)
    public static class CounterFields
    {
        public const string Reactions = "reactions";
        public const string Comments = "comments";
        public const string Reposts = "reposts";
    }
}