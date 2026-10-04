using System;

namespace SearchService.Indexes;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchService.Domain;
using SearchService.Helpers.ElasticsearchConfigurations;

public class PostsIndexInitializer : IHostedService
{
    private readonly ElasticsearchClient _es;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<PostsIndexInitializer> _logger;

    public PostsIndexInitializer(
        ElasticsearchClient es,
        IOptions<ElasticsearchOptions> options,
        ILogger<PostsIndexInitializer> logger)
    {
        _es = es;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsurePostsIndexAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    private async Task EnsurePostsIndexAsync(CancellationToken ct)
    {

        var indexExists = await _es.Indices
            .ExistsAsync(_options.PostsIndexName, ct);

        if (!indexExists.Exists)
        {
            _logger.LogInformation(
                "Creating posts index: {IndexName}", _options.PostsIndexName);

            var createResponse = await _es.Indices.CreateAsync(
                _options.PostsIndexName,
                c => c
                    .Settings(s => s
                        .NumberOfShards(_options.Shards)
                        .NumberOfReplicas(_options.Replicas))
                    .Mappings<PostDocument>(m => ConfigurePostMappings(m)),
                ct);

            if (!createResponse.IsValidResponse)
            {
                _logger.LogError(
                    "Failed to create posts index: {Debug}",
                    createResponse.DebugInformation);
                throw new InvalidOperationException(
                    "Failed to create posts index in Elasticsearch.");
            }

            _logger.LogInformation(
                "Posts index '{IndexName}' created successfully",
                _options.PostsIndexName);
        }


        var aliasExists = await _es.Indices
            .ExistsAliasAsync(_options.PostsIndexAlias, ct);

        if (!aliasExists.Exists)
        {
            _logger.LogInformation(
                "Creating alias {Alias} → {Index}",
                _options.PostsIndexAlias, _options.PostsIndexName);

            var aliasResponse = await _es.Indices.PutAliasAsync(
                _options.PostsIndexName,
                _options.PostsIndexAlias,
                ct);

            if (!aliasResponse.IsValidResponse)
            {
                _logger.LogError(
                    "Failed to create alias: {Debug}",
                    aliasResponse.DebugInformation);
                throw new InvalidOperationException(
                    "Failed to create posts alias in Elasticsearch.");
            }
        }
    }

    private static void ConfigurePostMappings(
        TypeMappingDescriptor<PostDocument> m)
    {
        m.Properties(p => p
            .LongNumber(n => n.Id)
            .LongNumber(n => n.AuthorId)
        
            .Text(t => t.Content, txt => txt
                .Analyzer("standard"))

            .Keyword(k => k.Hashtags)
            .LongNumber(n => n.MentionedUserIds)

            .Date(d => d.CreatedAt)
            .Date(d => d.UpdatedAt)
            .Date(d => d.IndexedAt));
    }
}