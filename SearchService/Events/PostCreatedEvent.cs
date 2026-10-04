namespace SearchService.Events;

public record PostCreatedEvent(
    long PostId,
    string AuthorId,
    string? Content,
    string Visibility,
    long? QuotedPostId,
    List<string>? MentionedUserIds,
    List<string>? Hashtags,
    DateTime CreatedAt);