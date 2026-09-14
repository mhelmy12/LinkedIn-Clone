using System;

namespace PostService.Models;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string AggregateType { get; set; } = default!; // Post
    public string AggregateId { get; set; } = default!;   // PostId (Kafka Partition Key)
    public string Type { get; set; } = default!;          // PostCreated
    public string Payload { get; set; } = default!;       // Json payload
    public DateTime Timestamp { get; set; }

}

// {
//   "connector.class": "io.debezium.connector.sqlserver.SqlServerConnector",
//   "table.include.list": "dbo.OutboxMessages",

//   "transforms": "outbox",
//   "transforms.outbox.type": "io.debezium.transforms.outbox.EventRouter",
//   "transforms.outbox.route.by.field": "aggregatetype",
//   "transforms.outbox.route.topic.replacement": "outbox.event.${routedByValue}",
//   "transforms.outbox.aggregate.id.variable": "aggregateid"
// }