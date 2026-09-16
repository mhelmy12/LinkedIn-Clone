namespace SearchService.Events;

public record PostCreatedEvent(
    long PostId,
    string AuthorId,
    string? Content,
    string Visibility,
    long? QuotedPostId,
    IReadOnlyList<string>? MentionedUserIds,
    IReadOnlyList<string>? Hashtags,
    DateTime CreatedAt);