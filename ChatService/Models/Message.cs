namespace ChatService.Models;

/// <summary>
/// A persisted, end-to-end encrypted chat message. The server never sees
/// plaintext content (FR-021): only <see cref="Ciphertext"/>,
/// <see cref="Nonce"/> and <see cref="Algorithm"/> are stored.
/// </summary>
public class Message
{
    public long Id { get; set; }
    public long ConversationId { get; set; }

    /// <summary>Keycloak subject of the sender.</summary>
    public string SenderId { get; set; } = default!;

    /// <summary>Client-generated id that makes sends and retries idempotent (FR-019/FR-023).</summary>
    public Guid ClientMessageId { get; set; }

    public byte[] Ciphertext { get; set; } = default!;
    public byte[] Nonce { get; set; } = default!;
    public string Algorithm { get; set; } = default!;
    public DateTime SentAt { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public Conversation? Conversation { get; set; }
}
