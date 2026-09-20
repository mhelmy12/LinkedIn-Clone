namespace PostService.Events;

public record PostUpdatedEvent(
    string PostId,
    string AuthorId,
    string? Content,
    int Visibility,
    List<string> Hashtags,
    List<string> NewMentionedUserIds,
    List<string> OldMentionedUserIds,
    bool ContentChanged,
    bool VisibilityChanged,
    bool MediaChanged,
    DateTimeOffset UpdatedAt);
