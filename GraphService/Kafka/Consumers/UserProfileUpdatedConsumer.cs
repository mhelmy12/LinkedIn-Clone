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

public class UserProfileUpdatedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<UserProfileUpdatedConsumer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UserProfileUpdatedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<UserProfileUpdatedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = "graph-service-user-profile-updated",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.UserServiceTopic);

        _logger.LogInformation(
            "UserProfileUpdatedConsumer started. Group: graph-service-user-profile-updated");

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
                        "Error processing UserProfileUpdated. Offset will NOT be committed.");
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

        if (envelope.After.Type != "UserProfileUpdated") return;

        var @event = JsonSerializer.Deserialize<UserProfileUpdatedEvent>(
            envelope.After.Payload, JsonOptions);

        if (@event is null) return;

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGraphRepository>();

        await repo.UpdateUserProfileAsync(
            @event.UserId,
            @event.FirstName + " " + @event.LastName,
            @event.ProfileImageKey,
            @event.Headline,
            ct);

        _logger.LogDebug(
            "UserProfileUpdated processed: UserId={UserId}", @event.UserId);
    }
}

public record UserProfileUpdatedEvent(
    string UserId,
    string? FirstName,
    string? LastName,
    string? Headline,
    string? JobTitle,
    string? Email,
    DateTime UpdatedAt,
    string? ProfileImageKey
);

