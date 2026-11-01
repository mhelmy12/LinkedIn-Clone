namespace ChatService.Models;

/// <summary>
/// Read model fed by User Service profile events. <see cref="UserId"/> is the
/// Keycloak subject, the repository-wide user identity, and provides the
/// display name / email needed by conversation lists and notifications.
/// </summary>
public class ChatUserProjection
{
    public string UserId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? Email { get; set; }
    public DateTime UpdatedAt { get; set; }
}
