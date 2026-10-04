using System;


using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchService.Domain;
using SearchService.Helpers.KafkaConfiguration;
using SearchService.Indexes;
namespace SearchService.Consumers;

public class CommentCreatedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<CommentCreatedConsumer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CommentCreatedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<CommentCreatedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = "search-service-comment-created",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.EngagementServiceTopic);

        _logger.LogInformation(
            "CommentCreatedConsumer started. Topic: {Topic}, Group: search-service-comment-created",
            _options.EngagementServiceTopic);

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
                    _logger.LogError(ex, "Error processing CommentCreated event");
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

        if (envelope.After.Type != "CommentCreated") return;

        var @event = JsonSerializer.Deserialize<CommentCreatedEvent>(
            envelope.After.Payload, JsonOptions);

        if (@event is null)
        {
            _logger.LogWarning("Failed to deserialize CommentCreatedEvent");
            return;
        }

        var document = new CommentDocument
        {
            Id = @event.CommentId,
            PostId = @event.PostId,
            AuthorId = @event.AuthorId,
            ParentCommentId = @event.ParentCommentId,
            Depth = @event.ParentCommentId.HasValue ? 1 : 0,
            Content = @event.Content,
            MentionedUserIds = @event.MentionedUserIds ?? new List<string>(),
            CreatedAt = @event.CreatedAt,
            IndexedAt = DateTime.UtcNow
        };

        using var scope = _scopeFactory.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<ICommentIndexer>();

        await indexer.IndexAsync(document, ct);
    }
}
public record CommentCreatedEvent(
    long CommentId,
    long PostId,
    string AuthorId,
    long? ParentCommentId,
    string? Content,
    List<string> MentionedUserIds,
    DateTime CreatedAt);