namespace ChatService.Events;

/// <summary>
/// Copied from the User Service payload. <see cref="UserId"/> is the Keycloak
/// <c>sub</c>, stored as-is in <c>ChatUserProjection</c>.
/// </summary>
public class UserProfileUpdatedEvent
{
    public string UserId { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Headline { get; set; }

    public string? JobTitle { get; set; }

    public string? Email { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? ProfileImageKey { get; set; }
}
