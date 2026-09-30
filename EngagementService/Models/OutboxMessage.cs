using System;

namespace EngagementService.Models;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string AggregateType { get; set; } = default!;
    public string AggregateId { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}
