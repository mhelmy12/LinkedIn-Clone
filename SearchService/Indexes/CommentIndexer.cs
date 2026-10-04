using System;

namespace SearchService.Indexes;

using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchService.Domain;
using SearchService.Helpers.ElasticsearchConfigurations;

public class CommentIndexer : ICommentIndexer
{
    private readonly ElasticsearchClient _es;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<CommentIndexer> _logger;

    public CommentIndexer(
        ElasticsearchClient es,
        IOptions<ElasticsearchOptions> options,
        ILogger<CommentIndexer> logger)
    {
        _es = es;
        _options = options.Value;
        _logger = logger;
    }

    public async Task IndexAsync(CommentDocument document, CancellationToken ct)
    {
        document.IndexedAt = DateTime.UtcNow;

        var response = await _es.IndexAsync(
            document,
            idx => idx
                .Index(_options.CommentsIndexAlias)
                .Id(document.Id.ToString()),
            ct);

        if (!response.IsValidResponse)
        {
            _logger.LogError(
                "Failed to index comment {CommentId}: {Debug}",
                document.Id, response.DebugInformation);

            throw new InvalidOperationException(
                $"Failed to index comment {document.Id}.");
        }

        _logger.LogInformation(
            "Indexed comment {CommentId} (result: {Result})",
            document.Id, response.Result);
    }

    public async Task DeleteAsync(long commentId, CancellationToken ct)
    {
        var response = await _es.DeleteAsync<CommentDocument>(
            commentId.ToString(),
            d => d.Index(_options.CommentsIndexAlias),
            ct);

        if (!response.IsValidResponse && response.Result != Result.NotFound)
        {
            _logger.LogError(
                "Failed to delete comment {CommentId}: {Debug}",
                commentId, response.DebugInformation);

            throw new InvalidOperationException(
                $"Failed to delete comment {commentId}.");
        }

        _logger.LogInformation(
            "Deleted comment {CommentId} (result: {Result})",
            commentId, response.Result);
    }

    public async Task DeleteManyAsync(
        IReadOnlyList<long> commentIds,
        CancellationToken ct)
    {
        if (commentIds.Count == 0) return;

        var idsToDelete = commentIds
            .Select(id => id.ToString())
            .ToArray();
        var response = await _es.DeleteByQueryAsync<CommentDocument>(
            d => d
                .Indices(_options.CommentsIndexAlias)
                .Query(q => q
                    .Ids(i => i.Values(idsToDelete)))
                .Refresh(true),
            ct);

        if (!response.IsValidResponse)
        {
            _logger.LogError(
                "Failed to delete {Count} comments: {Debug}",
                commentIds.Count, response.DebugInformation);

            throw new InvalidOperationException(
                $"Failed to bulk delete {commentIds.Count} comments.");
        }

        _logger.LogInformation(
            "Deleted {Count} comments from index (deleted: {Deleted})",
            commentIds.Count, response.Deleted);
    }

    public async Task UpdateAuthorInfoAsync(
        long authorId,
        string? displayName,
        string? profileImageKey,
        string? headline,
        CancellationToken ct)
    {
        var response = await _es.UpdateByQueryAsync<CommentDocument>(
            u => u
                .Indices(_options.CommentsIndexAlias)
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.AuthorId)
                        .Value(authorId)))
                .Script(s => s
                    .Source(
                        "ctx._source.authorDisplayName = params.displayName; " +
                        "ctx._source.authorProfileImageKey = params.profileImageKey; " +
                        "ctx._source.authorHeadline = params.headline;")
                    .Params(p => p
                        .Add("displayName", displayName)
                        .Add("profileImageKey", profileImageKey)
                        .Add("headline", headline))),
            ct);

        _logger.LogInformation(
            "Updated author info for {Count} comments (authorId: {AuthorId})",
            response.Updated, authorId);
    }
}