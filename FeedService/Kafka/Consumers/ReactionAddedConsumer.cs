using System;


using System.Text.Json;
using Confluent.Kafka;
using FeedService.Abstractions;
using FeedService.Helpers.KafkaConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FeedService.Kafka.Consumers;

public class ReactionAddedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<ReactionAddedConsumer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const int PostTargetType = 1;

    public ReactionAddedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ReactionAddedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = "feed-service-reaction-added",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.EngagementServiceTopic);

        _logger.LogInformation(
            "ReactionAddedConsumer started. Group: feed-service-reaction-added");

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
                        "Error processing ReactionAdded. Offset will NOT be committed.");
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

        if (envelope.After.Type != "ReactionAdded") return;

        var @event = JsonSerializer.Deserialize<ReactionAddedEvent>(
            envelope.After.Payload, JsonOptions);

        if (@event is null) return;

        if (@event.TargetType != PostTargetType) return;

        using var scope = _scopeFactory.CreateScope();
        var countersCache = scope.ServiceProvider
            .GetRequiredService<ICountersCache>();
        var postId = long.Parse(@event.TargetId);
        await countersCache.IncrementReactionsAsync(postId, 1, ct);

        _logger.LogDebug(
            "ReactionAdded processed: Post {PostId} reactions +1",
            @event.TargetId);
    }
}

public record ReactionAddedEvent(
    string ReactionId,
    string UserId,
    int TargetType,
    string TargetId,
    int ReactionType,
    DateTime CreatedAt);
