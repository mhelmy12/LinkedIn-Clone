using System;

namespace GraphService.Kafka.Consumers;

using System.Text.Json;
using Confluent.Kafka;
using GraphService.Abstractions;
using GraphService.Kafka.KafkaConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


public class ConnectionRemovedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<ConnectionRemovedConsumer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ConnectionRemovedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ConnectionRemovedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = "graph-service-connection-removed",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.UserServiceTopic);

        _logger.LogInformation(
            "ConnectionRemovedConsumer started. Group: graph-service-connection-removed");

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
                        "Error processing ConnectionRemoved. Offset will NOT be committed.");
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

        if (envelope.After.Type != "ConnectionRemoved") return;

        var @event = JsonSerializer.Deserialize<ConnectionRemovedEvent>(
            envelope.After.Payload, JsonOptions);

        if (@event is null) return;

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGraphRepository>();

        await repo.RemoveConnectionAsync(
            @event.UserId1,
            @event.UserId2,
            ct);

        _logger.LogInformation(
            "ConnectionRemoved processed: {User1} <-> {User2}",
            @event.UserId1, @event.UserId2);
    }
}
public record ConnectionRemovedEvent(
    string UserId1,
    string UserId2,
    DateTime RemovedAt);
