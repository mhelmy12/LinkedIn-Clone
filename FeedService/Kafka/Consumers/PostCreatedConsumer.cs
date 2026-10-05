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
            GroupId = "feed-service-post-created",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.PostServiceTopic);

        _logger.LogInformation(
            "PostCreatedConsumer started. Topic: {Topic}, Group: feed-service-post-created",
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
                        "Error processing PostCreated. Offset will NOT be committed.");
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

        if (envelope.After.Type != "PostCreated") return;

        var @event = JsonSerializer.Deserialize<PostCreatedEvent>(
            envelope.After.Payload, JsonOptions);

        if (@event is null)
        {
            _logger.LogWarning("Failed to deserialize PostCreatedEvent");
            return;
        }

        using var scope = _scopeFactory.CreateScope();

        var snapshotStore = scope.ServiceProvider
            .GetRequiredService<ISnapshotStore>();
        var userSummaryStore = scope.ServiceProvider
            .GetRequiredService<IUserSummaryStore>();
        var fanOutService = scope.ServiceProvider
            .GetRequiredService<IFeedFanOutService>();
        var countersCache = scope.ServiceProvider
            .GetRequiredService<ICountersCache>();

        var isPureRepost = @event.Content is null && @event.QuotedPostId.HasValue;

        var snapshot = new PostSnapshot
        {
            PostId = @event.PostId,
            AuthorId = @event.AuthorId,
            Content = @event.Content,
            Visibility = @event.Visibility,
            RepostOfPostId = @event.QuotedPostId,
            IsPureRepost = isPureRepost,
            Hashtags = @event.Hashtags ?? new List<string>(),
            MentionedUserIds = @event.MentionedUserIds ?? new List<string>(),
            CreatedAt = @event.CreatedAt,
            IsDeleted = false
        };

        var authorSummary = await userSummaryStore.GetAsync(@event.AuthorId, ct);
        if (authorSummary is not null)
        {
            snapshot.AuthorDisplayName = authorSummary.DisplayName;
            snapshot.AuthorProfileImageKey = authorSummary.ProfileImageKey;
            snapshot.AuthorHeadline = authorSummary.Headline;
        }

        if (@event.QuotedPostId.HasValue)
        {
            var original = await snapshotStore
                .GetAsync(@event.QuotedPostId.Value, ct);

            if (original is not null)
            {
                snapshot.RepostOfContent = original.Content;
                snapshot.RepostOfAuthorId = original.AuthorId;
                snapshot.RepostOfAuthorDisplayName = original.AuthorDisplayName;
            }
        }

        await snapshotStore.SaveAsync(snapshot, ct);

        if (@event.QuotedPostId.HasValue)
        {
            await countersCache.IncrementRepostsAsync(
                @event.QuotedPostId.Value, 1, ct);
        }

        await fanOutService.FanOutAsync(snapshot, ct);

        _logger.LogInformation(
            "PostCreated processed: PostId={PostId}, Author={AuthorId}, IsRepost={IsRepost}, FanOut recipients done",
            @event.PostId, @event.AuthorId, isPureRepost);
    }
}

public record PostCreatedEvent(
    long PostId,
    string AuthorId,
    string? Content,
    int Visibility,
    long? QuotedPostId,
    List<string>? MentionedUserIds,
    List<string>? Hashtags,
    DateTime CreatedAt);