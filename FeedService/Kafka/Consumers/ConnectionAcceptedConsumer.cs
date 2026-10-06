using System.Text.Json;
using Confluent.Kafka;
using FeedService.Abstractions;
using FeedService.Helpers.KafkaConfiguration;
using FeedService.Helpers.RedisConfiguration;
using Microsoft.Extensions.Options;


namespace FeedService.Kafka.Consumers;

public class ConnectionAcceptedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _kafkaOptions;
    private readonly FeedOptions _feedOptions;
    private readonly ILogger<ConnectionAcceptedConsumer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ConnectionAcceptedConsumer(
        IOptions<KafkaOptions> kafkaOptions,
        IOptions<FeedOptions> feedOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<ConnectionAcceptedConsumer> logger)
    {
        _kafkaOptions = kafkaOptions.Value;
        _feedOptions = feedOptions.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = "feed-service-connection-accepted",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_kafkaOptions.UserServiceTopic);

        _logger.LogInformation(
            "ConnectionAcceptedConsumer started. Group: feed-service-connection-accepted");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(
                        TimeSpan.FromMilliseconds(_kafkaOptions.ConsumerPollTimeoutMs));

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
                        "Error processing ConnectionAccepted. Offset will NOT be committed.");
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

        if (envelope.After.Type != "ConnectionAccepted") return;

        var @event = JsonSerializer.Deserialize<ConnectionAcceptedEvent>(
            envelope.After.Payload, JsonOptions);

        if (@event is null) return;

        using var scope = _scopeFactory.CreateScope();

        var connectionsCache = scope.ServiceProvider
            .GetRequiredService<IConnectionsCache>();
        var snapshotStore = scope.ServiceProvider
            .GetRequiredService<ISnapshotStore>();
        var fanOutService = scope.ServiceProvider
            .GetRequiredService<IFeedFanOutService>();


        await connectionsCache.AddConnectionAsync(
            @event.UserId1, @event.UserId2, ct);

        _logger.LogInformation(
            "Connection accepted: {User1} ↔ {User2}",
            @event.UserId1, @event.UserId2);


        // Initial fan-out of recent posts between the two users

        //1. from User1 → feed User2
        var recent1 = await snapshotStore.GetRecentByAuthorAsync(
            @event.UserId1,
            _feedOptions.InitialFanOutSize,
            ct);

        if (recent1.Count > 0)
        {
            await fanOutService.FanOutToUserAsync(
                @event.UserId2, recent1, ct);
        }

        //2. from User2 → feed User1
        var recent2 = await snapshotStore.GetRecentByAuthorAsync(
            @event.UserId2,
            _feedOptions.InitialFanOutSize,
            ct);

        if (recent2.Count > 0)
        {
            await fanOutService.FanOutToUserAsync(
                @event.UserId1, recent2, ct);
        }

        _logger.LogInformation(
            "Initial fan-out done: {User1}→{User2} ({Count1} posts), {User2}→{User1} ({Count2} posts)",
            @event.UserId1, @event.UserId2, recent1.Count,
            @event.UserId2, @event.UserId1, recent2.Count);
    }
}
public record ConnectionAcceptedEvent(
    string UserId1,
    string UserId2,
    DateTime AcceptedAt);