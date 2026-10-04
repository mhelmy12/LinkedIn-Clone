using System;
using System.Text.Json;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using SearchService.Domain;
using SearchService.Events;
using SearchService.Helpers.KafkaConfiguration;
using SearchService.Indexes;

namespace SearchService.Infrastructure.Kafka.Consumers;

public class PostCreatedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<PostCreatedConsumer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PostCreatedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<PostCreatedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = "search-service-post-created",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.PostServiceTopic);

        _logger.LogInformation(
            "PostCreatedConsumer started. Topic: {Topic}, Group: search-service-post-created",
            _options.PostServiceTopic);

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
                    _logger.LogError(ex,
                        "Error processing message. Offset will not be committed.");
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
        // 1. Parse Debezium envelope
        var envelope = JsonSerializer.Deserialize<DebeziumEnvelope>(rawJson, JsonOptions);
        if (envelope?.After is null) return;

        // 2. Filter: PostCreated only
        if (envelope.After.Type != "PostCreated") return;

        // 3. Parse payload
        var @event = JsonSerializer.Deserialize<PostCreatedEvent>(
            envelope.After.Payload, JsonOptions);

        if (@event is null)
        {
            _logger.LogWarning("Failed to deserialize PostCreatedEvent");
            return;
        }

        // 4. Build document
        var document = new PostDocument
        {
            Id = @event.PostId,
            AuthorId = @event.AuthorId,
            Content = @event.Content,
            Hashtags = @event.Hashtags ?? new List<string>(),
            MentionedUserIds = @event.MentionedUserIds ?? new List<string>(),
            CreatedAt = @event.CreatedAt,
            IndexedAt = DateTime.UtcNow
        };

        using var scope = _scopeFactory.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<IPostIndexer>();

        await indexer.IndexAsync(document, ct);
    }
}