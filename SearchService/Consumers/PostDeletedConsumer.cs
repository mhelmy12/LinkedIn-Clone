using System;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using SearchService.Helpers.KafkaConfiguration;
using SearchService.Indexes;

namespace SearchService.Consumers;

public class PostDeletedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<PostDeletedConsumer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PostDeletedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<PostDeletedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = "search-service-post-deleted",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.PostServiceTopic);

        _logger.LogInformation(
            "PostDeletedConsumer started. Group: search-service-post-deleted");

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
                    _logger.LogError(ex, "Error processing PostDeleted event");
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

        if (envelope.After.Type != "PostDeleted") return;

        var @event = JsonSerializer.Deserialize<PostDeletedEvent>(
            envelope.After.Payload, JsonOptions);

        if (@event is null) return;

        if (!long.TryParse(@event.PostId, out var postId))
        {
            _logger.LogWarning(
                "Invalid PostId in PostDeleted event: {PostId}", @event.PostId);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<IPostIndexer>();

        await indexer.DeleteAsync(postId, ct);
    }
}
public record PostDeletedEvent(
    string PostId,
    string AuthorId,
    DateTimeOffset DeletedAt);