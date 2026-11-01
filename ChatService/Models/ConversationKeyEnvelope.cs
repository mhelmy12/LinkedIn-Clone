namespace ChatService.Models;

/// <summary>
/// A conversation key wrapped for one recipient device. The service treats
/// the payload as opaque ciphertext; only the addressed device can unwrap it.
/// </summary>
public class ConversationKeyEnvelope
{
    public long ConversationId { get; set; }
    public Guid DeviceId { get; set; }
    public byte[] WrappedKey { get; set; } = default!;
    public byte[] Nonce { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
