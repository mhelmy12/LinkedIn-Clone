namespace ChatService.Models;

/// <summary>
/// Read model of accepted connections, rebuilt from User Service events.
/// Each accepted pair produces two rows (one per direction) with a normalized
/// <see cref="OtherUserId"/> ordering.
/// </summary>
public class ChatConnectionProjection
{
    /// <summary>Keycloak subject of the owning user.</summary>
    public string UserId { get; set; } = default!;

    /// <summary>Keycloak subject of the connected peer.</summary>
    public string OtherUserId { get; set; } = default!;

    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}
