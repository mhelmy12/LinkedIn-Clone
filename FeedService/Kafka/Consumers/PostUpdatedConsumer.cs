using System;


using System.Text.Json;
using Confluent.Kafka;
using FeedService.Abstractions;
using FeedService.Helpers.KafkaConfiguration;
using FeedService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FeedService.Kafka.Consumers;

public class PostUpdatedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<PostUpdatedConsumer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PostUpdatedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<PostUpdatedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = "feed-service-post-updated",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.PostServiceTopic);

        _logger.LogInformation(
            "PostUpdatedConsumer started. Group: feed-service-post-updated");

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
                        "Error processing PostUpdated. Offset will NOT be committed.");
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

        if (envelope.After.Type != "PostUpdated") return;

        var @event = JsonSerializer.Deserialize<PostUpdatedEvent>(
            envelope.After.Payload, JsonOptions);

        if (@event is null)
        {
            _logger.LogWarning("Failed to deserialize PostUpdatedEvent");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var snapshotStore = scope.ServiceProvider
            .GetRequiredService<ISnapshotStore>();

        var postId = long.Parse(@event.PostId);
        var existing = await snapshotStore.GetAsync(postId, ct);

        if (existing is null)
        {
            _logger.LogWarning(
                "PostSnapshot {PostId} not found for update — skipping",
                @event.PostId);
            return;
        }

        if (@event.ContentChanged)
        {
            existing.Content = @event.Content;
            existing.Hashtags = @event.Hashtags ?? new List<string>();
        }

        if (@event.VisibilityChanged)
        {
            existing.Visibility = @event.Visibility;
        }

        existing.MentionedUserIds = @event.NewMentionedUserIds ?? new List<string>();

        existing.UpdatedAt = @event.UpdatedAt;

        await snapshotStore.UpdateAsync(existing, ct);

        _logger.LogInformation(
            "PostUpdated processed: PostId={PostId}, ContentChanged={ContentChanged}, VisibilityChanged={VisibilityChanged}",
            @event.PostId, @event.ContentChanged, @event.VisibilityChanged);
    }
}
public record PostUpdatedEvent(
    string PostId,
    string AuthorId,
    string? Content,
    int Visibility,
    List<string> Hashtags,
    List<string> NewMentionedUserIds,
    List<string> OldMentionedUserIds,
    bool ContentChanged,
    bool VisibilityChanged,
    bool MediaChanged,
    DateTime UpdatedAt);

