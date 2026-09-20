namespace PostService.Events;

public record PostDeletedEvent(
    string PostId,
    string AuthorId,
    List<string> RepostedByUserIds,
    DateTimeOffset DeletedAt);