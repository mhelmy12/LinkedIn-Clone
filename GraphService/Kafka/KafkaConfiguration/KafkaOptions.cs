using System;

namespace GraphService.Kafka.KafkaConfiguration;

public class KafkaOptions
{
    public const string SectionName = "Kafka";
    public string BootstrapServers { get; set; } = "localhost:9094";

    public bool EnableAutoCommit { get; set; } = false;

    public string UserServiceTopic { get; set; } = "userService.LinkedInPostDb.dbo.OutboxMessages";

    public int ConsumerPollTimeoutMs { get; set; } = 500;

}
