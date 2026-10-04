using System;

namespace SearchService.Consumers;

using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchService.Helpers.KafkaConfiguration;
using SearchService.Indexes;

public class CommentDeletedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<CommentDeletedConsumer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CommentDeletedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<CommentDeletedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = "search-service-comment-deleted",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.EngagementServiceTopic);

        _logger.LogInformation(
            "CommentDeletedConsumer started. Group: search-service-comment-deleted");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(
                        TimeSpan.FromMilliseconds(_options.ConsumerPollTimeoutMs));

                    if (result?.Message?.Value is null)
                        continue;

                    await ProcessAsync(result.Message.Value, stoppingToken);
                    _consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error: {Reason}", ex.Error.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing CommentDeleted event");
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task ProcessAsync(string rawJson, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<DebeziumEnvelope>(rawJson, JsonOptions);
        if (envelope?.After is null) return;

        if (envelope.After.Type != "CommentDeleted") return;

        var @event = JsonSerializer.Deserialize<CommentDeletedEvent>(
            envelope.After.Payload, JsonOptions);

        if (@event is null) return;

        using var scope = _scopeFactory.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<ICommentIndexer>();

        await indexer.DeleteManyAsync(@event.DeletedCommentIds, ct);

        _logger.LogInformation(
            "Processed CommentDeleted: {Count} comments removed (root: {RootId})",
            @event.DeletedCommentIds.Count, @event.CommentId);
    }
}

public record CommentDeletedEvent(
    long CommentId,           // root
    long PostId,
    string AuthorId,
    long? ParentCommentId,
    List<long> DeletedCommentIds,   // all deleted (root + descendants)
    DateTime DeletedAt);
