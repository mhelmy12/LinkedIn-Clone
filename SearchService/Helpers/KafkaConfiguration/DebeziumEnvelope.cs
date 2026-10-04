using System;

namespace SearchService.Helpers.KafkaConfiguration;

public class DebeziumEnvelope
{
    public OutboxPayload? Before { get; set; }
    public OutboxPayload? After { get; set; }
    public string Op { get; set; } = default!;
}

public class OutboxPayload
{
    public string Id { get; set; } = default!;
    public string AggregateId { get; set; } = default!;
    public string? AggregateType { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}