namespace PostService.Events;

public record PostDeletedEvent(
    string PostId,
    string AuthorId,
    DateTimeOffset DeletedAt);