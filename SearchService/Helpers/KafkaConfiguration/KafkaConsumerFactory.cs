using System;
using Confluent.Kafka;

namespace SearchService.Helpers.KafkaConfiguration;

public static class KafkaConsumerFactory
{
    public static IConsumer<string, string> Create(
        string bootstrapServers,
        string groupId)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnablePartitionEof = true,
            BrokerAddressFamily = BrokerAddressFamily.V4,
        };
        return new ConsumerBuilder<string, string>(config).Build();
    }
}