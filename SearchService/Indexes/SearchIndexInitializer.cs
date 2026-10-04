using System;

namespace SearchService.Indexes;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchService.Domain;
using SearchService.Helpers.ElasticsearchConfigurations;

public class SearchIndexesInitializer : IHostedService
{
    private readonly ElasticsearchClient _es;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<SearchIndexesInitializer> _logger;

    public SearchIndexesInitializer(
        ElasticsearchClient es,
        IOptions<ElasticsearchOptions> options,
        ILogger<SearchIndexesInitializer> logger)
    {
        _es = es;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureIndexAsync<PostDocument>(
            physicalIndex: _options.PostsIndexName,
            alias: _options.PostsIndexAlias,
            mappingsConfigurator: ConfigurePostMappings,
            ct: cancellationToken);

        await EnsureIndexAsync<CommentDocument>(
            physicalIndex: _options.CommentsIndexName,
            alias: _options.CommentsIndexAlias,
            mappingsConfigurator: ConfigureCommentMappings,
            ct: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureIndexAsync<T>(
        string physicalIndex,
        string alias,
        Action<TypeMappingDescriptor<T>> mappingsConfigurator,
        CancellationToken ct) where T : class
    {
        var indexExists = await _es.Indices.ExistsAsync(physicalIndex, ct);

        if (!indexExists.Exists)
        {
            _logger.LogInformation("Creating index: {Index}", physicalIndex);

            var createResponse = await _es.Indices.CreateAsync(physicalIndex, c => c
                .Settings(s => s
                    .NumberOfShards(_options.Shards)
                    .NumberOfReplicas(_options.Replicas))
                .Mappings<T>(m => mappingsConfigurator(m)),
                ct);

            if (!createResponse.IsValidResponse)
            {
                _logger.LogError(
                    "Failed to create index {Index}: {Debug}",
                    physicalIndex, createResponse.DebugInformation);
                throw new InvalidOperationException(
                    $"Failed to create index {physicalIndex}.");
            }

            _logger.LogInformation("Index {Index} created", physicalIndex);
        }

        var aliasExists = await _es.Indices.ExistsAliasAsync(alias, ct);

        if (!aliasExists.Exists)
        {
            _logger.LogInformation(
                "Creating alias {Alias} → {Index}", alias, physicalIndex);

            var aliasResponse = await _es.Indices.PutAliasAsync(
                physicalIndex, alias, ct);

            if (!aliasResponse.IsValidResponse)
            {
                _logger.LogError(
                    "Failed to create alias {Alias}: {Debug}",
                    alias, aliasResponse.DebugInformation);
                throw new InvalidOperationException(
                    $"Failed to create alias {alias}.");
            }
        }
    }

    // Posts mappings
    private static void ConfigurePostMappings(TypeMappingDescriptor<PostDocument> m)
    {
        m.Properties(p => p
            .LongNumber(n => n.Id)
            .LongNumber(n => n.AuthorId)
            .Text(t => t.Content, txt => txt.Analyzer("standard"))
            .Keyword(k => k.Hashtags)
            .LongNumber(n => n.MentionedUserIds)
            .Date(d => d.CreatedAt)
            .Date(d => d.UpdatedAt)
            .Date(d => d.IndexedAt));
    }

    // Comments mappings
    private static void ConfigureCommentMappings(TypeMappingDescriptor<CommentDocument> m)
    {
        m.Properties(p => p
            .LongNumber(n => n.Id)
            .LongNumber(n => n.PostId)
            .LongNumber(n => n.AuthorId)
            .LongNumber(n => n.ParentCommentId)
            .IntegerNumber(i => i.Depth)

            .Text(t => t.AuthorDisplayName, txt => txt
                .Analyzer("standard")
                .Fields(f => f.Keyword("keyword")))
            .Keyword(k => k.AuthorProfileImageKey)
            .Text(t => t.AuthorHeadline)

            .Text(t => t.Content, txt => txt.Analyzer("standard"))

            .LongNumber(n => n.MentionedUserIds)

            .Date(d => d.CreatedAt)
            .Date(d => d.UpdatedAt)
            .Date(d => d.IndexedAt));
    }
}