namespace ChatService.Models;

/// <summary>
/// Consumer inbox row used to deduplicate at-least-once event delivery.
/// </summary>
public class InboxMessage
{
    public string ConsumerGroup { get; set; } = default!;
    public Guid EventId { get; set; }
    public DateTime ProcessedAt { get; set; }
}
