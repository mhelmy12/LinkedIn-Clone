using System.Text.Json;
using Confluent.Kafka;
using FeedService.Abstractions;
using FeedService.Helpers.KafkaConfiguration;

using Microsoft.Extensions.Options;


namespace FeedService.Kafka.Consumers;

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
            GroupId = "feed-service-post-deleted",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.PostServiceTopic);

        _logger.LogInformation(
            "PostDeletedConsumer started. Group: feed-service-post-deleted");

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
                        "Error processing PostDeleted. Offset will NOT be committed.");
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

        var snapshotStore = scope.ServiceProvider
            .GetRequiredService<ISnapshotStore>();
        var fanOutService = scope.ServiceProvider
            .GetRequiredService<IFeedFanOutService>();
        var countersCache = scope.ServiceProvider
            .GetRequiredService<ICountersCache>();

        var snapshot = await snapshotStore.GetAsync(postId, ct);

        if (snapshot is null)
        {
            _logger.LogWarning(
                "PostSnapshot {PostId} not found for delete — skipping",
                postId);
            return;
        }

        await fanOutService.RemoveFromFeedsAsync(
            postId, snapshot.AuthorId, ct);

        if (snapshot.RepostOfPostId.HasValue)
        {
            await countersCache.IncrementRepostsAsync(
                snapshot.RepostOfPostId.Value, -1, ct);
        }

        await countersCache.DeleteAsync(postId, ct);

        await snapshotStore.MarkDeletedAsync(postId, ct);

        _logger.LogInformation(
            "PostDeleted processed: PostId={PostId}, AuthorId={AuthorId}, WasRepost={WasRepost}",
            postId, snapshot.AuthorId, snapshot.RepostOfPostId.HasValue);
    }
}

public record PostDeletedEvent(
    string PostId,
    string AuthorId,
    DateTime DeletedAt);
