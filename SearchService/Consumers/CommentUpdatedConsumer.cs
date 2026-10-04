using System;


using System.Text.Json;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchService.Domain;
using SearchService.Helpers.KafkaConfiguration;
namespace SearchService.Consumers;

public class CommentUpdatedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<CommentUpdatedConsumer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CommentUpdatedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<CommentUpdatedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = "search-service-comment-updated",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.EngagementServiceTopic);

        _logger.LogInformation(
            "CommentUpdatedConsumer started. Group: search-service-comment-updated");

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
                    _logger.LogError(ex, "Error processing CommentUpdated event");
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

        if (envelope.After.Type != "CommentUpdated") return;

        var @event = JsonSerializer.Deserialize<CommentUpdatedEvent>(
            envelope.After.Payload, JsonOptions);

        if (@event is null) return;

        using var scope = _scopeFactory.CreateScope();
        var es = scope.ServiceProvider.GetRequiredService<ElasticsearchClient>();

        var response = await es.UpdateAsync<CommentDocument, object>(
            "comments",
            new Id(@event.CommentId.ToString()),
            u => u
                .Index("comments")
                .Doc(new
                {
                    content = @event.Content,
                    updatedAt = @event.UpdatedAt
                })
                .DocAsUpsert(false),
            ct);

        if (!response.IsValidResponse && response.Result != Result.NotFound)
        {
            _logger.LogError(
                "Failed to update comment {CommentId}: {Debug}",
                @event.CommentId, response.DebugInformation);
            throw new InvalidOperationException("Update failed.");
        }

        _logger.LogInformation(
            "Updated comment {CommentId} in index (result: {Result})",
            @event.CommentId, response.Result);
    }
}
public record CommentUpdatedEvent(
    long CommentId,
    long PostId,
    string AuthorId,
    long? ParentCommentId,
    string? Content,
    bool ContentChanged,
    DateTime UpdatedAt);