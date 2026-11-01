namespace ChatService.Models;

/// <summary>
/// A device's ECDH P-256 public key. Private keys never leave the client.
/// </summary>
public class UserDeviceKey
{
    public Guid DeviceId { get; set; }

    /// <summary>Keycloak subject that owns the device.</summary>
    public string UserId { get; set; } = default!;

    public byte[] PublicKey { get; set; } = default!;
    public string Algorithm { get; set; } = default!;
    public DateTime RegisteredAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
