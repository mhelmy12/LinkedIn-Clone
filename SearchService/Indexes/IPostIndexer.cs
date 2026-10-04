using System;
using SearchService.Domain;

namespace SearchService.Indexes;

public interface IPostIndexer
{
    Task IndexAsync(PostDocument document, CancellationToken ct);
    Task DeleteAsync(long postId, CancellationToken ct);
    Task UpdateAuthorInfoAsync(
        long authorId,
        CancellationToken ct);
}