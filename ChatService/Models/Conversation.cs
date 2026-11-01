namespace ChatService.Models;

/// <summary>
/// A single conversation between two connected users.
/// <see cref="Participant1Id"/> is always the ordinally lower Keycloak subject
/// and <see cref="Participant2Id"/> the higher one, so a user pair can only
/// produce one conversation (FR-003).
/// </summary>
public class Conversation
{
    public long Id { get; set; }
    public string Participant1Id { get; set; } = default!;
    public string Participant2Id { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
}
