using System;
using SearchService.Domain;

namespace SearchService.Indexes;


public interface ICommentIndexer
{
    Task IndexAsync(CommentDocument document, CancellationToken ct);

    Task DeleteAsync(long commentId, CancellationToken ct);

    Task DeleteManyAsync(
        IReadOnlyList<long> commentIds,
        CancellationToken ct);

    Task UpdateAuthorInfoAsync(
        long authorId,
        string? displayName,
        string? profileImageKey,
        string? headline,
        CancellationToken ct);
}