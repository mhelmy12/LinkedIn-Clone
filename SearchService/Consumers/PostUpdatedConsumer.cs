using System;

namespace SearchService.Consumers;

using System.Text.Json;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using SearchService.Domain;
using SearchService.Helpers.KafkaConfiguration;

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
            GroupId = "search-service-post-updated",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.PostServiceTopic);

        _logger.LogInformation(
            "PostUpdatedConsumer started. Group: search-service-post-updated");

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
                    _logger.LogError(ex, "Error processing PostUpdated event");
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
        var es = scope.ServiceProvider
            .GetRequiredService<ElasticsearchClient>();

        var updateRequest = new UpdateRequest<PostDocument, object>("posts", @event.PostId.ToString())
        {
            Doc = new
            {
                content = @event.Content,
                visibility = @event.Visibility,
                hashtags = @event.Hashtags,
                mentionedUserIds = @event.NewMentionedUserIds,
                updatedAt = @event.UpdatedAt
            },
            DocAsUpsert = false
        };

        var response = await es.UpdateAsync(updateRequest, ct);

        if (!response.IsValidResponse && response.Result != Result.NotFound)
        {
            _logger.LogError(
                "Failed to update post {PostId}: {Debug}",
                @event.PostId, response.DebugInformation);
            throw new InvalidOperationException("Update failed.");
        }

        _logger.LogInformation(
            "Updated post {PostId} in index (result: {Result})",
            @event.PostId, response.Result);
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
    DateTimeOffset UpdatedAt);