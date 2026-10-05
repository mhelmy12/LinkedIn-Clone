using System;

namespace FeedService.Helpers.KafkaConfiguration;

public class KafkaOptions
{
    public const string SectionName = "Kafka";
    public string BootstrapServers { get; set; } = "localhost:9094";

    public bool EnableAutoCommit { get; set; } = false;

    public string PostServiceTopic { get; set; } = "postService.LinkedInPostDb.dbo.OutboxMessages";
    public string UserServiceTopic { get; set; } = "userService.LinkedInPostDb.dbo.OutboxMessages";
    public string EngagementServiceTopic { get; set; } = "engagementService.LinkedInPostDb.public.OutboxMessages";

    public int ConsumerPollTimeoutMs { get; set; } = 500;

}
