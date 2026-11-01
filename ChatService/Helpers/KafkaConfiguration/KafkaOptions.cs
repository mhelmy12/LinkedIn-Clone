using Confluent.Kafka;

namespace ChatService.Helpers.KafkaConfiguration;

/// <summary>
/// Bound from the <c>Kafka</c> configuration section. Consumers never
/// auto-commit: an offset is committed only after the projection upsert
/// succeeds (Constitution III).
/// </summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9094";

    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;

    public bool EnableAutoCommit { get; set; } = false;

    /// <summary>User Service outbox topic published by Debezium.</summary>
    public string UserServiceTopic { get; set; } = "userService.LinkedInUserDb.dbo.OutboxMessages";

    public int ConsumerPollTimeoutMs { get; set; } = 500;

    /// <summary>Bounded retries before a message is sent to the dead-letter topic.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    public string DeadLetterTopic { get; set; } = "chat-service-dlq";
}
