using System;

namespace SearchService.Indexes;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchService.Domain;
using SearchService.Helpers.ElasticsearchConfigurations;

public class PostIndexer : IPostIndexer
{
    private readonly ElasticsearchClient _es;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<PostIndexer> _logger;

    public PostIndexer(
        ElasticsearchClient es,
        IOptions<ElasticsearchOptions> options,
        ILogger<PostIndexer> logger)
    {
        _es = es;
        _options = options.Value;
        _logger = logger;
    }

    public async Task IndexAsync(PostDocument document, CancellationToken ct)
    {
        document.IndexedAt = DateTime.UtcNow;

        var response = await _es.IndexAsync(
            document,
            idx => idx
                .Index(_options.PostsIndexAlias)
                .Id(document.Id.ToString()),
            ct);

        if (!response.IsValidResponse)
        {
            _logger.LogError(
                "Failed to index post {PostId}: {Debug}",
                document.Id, response.DebugInformation);

            throw new InvalidOperationException(
                $"Failed to index post {document.Id} in Elasticsearch.");
        }

        _logger.LogInformation(
            "Indexed post {PostId} (result: {Result})",
            document.Id, response.Result);
    }

    public async Task DeleteAsync(long postId, CancellationToken ct)
    {
        var response = await _es.DeleteAsync<PostDocument>(
            postId.ToString(),
            d => d.Index(_options.PostsIndexAlias),
            ct);

        if (!response.IsValidResponse && response.Result != Result.NotFound)
        {
            _logger.LogError(
                "Failed to delete post {PostId} from index: {Debug}",
                postId, response.DebugInformation);

            throw new InvalidOperationException(
                $"Failed to delete post {postId} from Elasticsearch.");
        }

        _logger.LogInformation(
            "Deleted post {PostId} from index (result: {Result})",
            postId, response.Result);
    }

    public async Task UpdateAuthorInfoAsync(
        long authorId,
        CancellationToken ct)
    {
        var response = await _es.UpdateByQueryAsync<PostDocument>(
            u => u
                .Indices(_options.PostsIndexAlias)
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.AuthorId)
                        .Value(authorId))), ct);

        _logger.LogInformation(
            "Updated author info for {Count} posts (authorId: {AuthorId})",
            response.Updated, authorId);
    }
}