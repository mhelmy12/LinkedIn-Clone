using System;

namespace FeedService.Kafka.Consumers;

using System.Text.Json;
using Confluent.Kafka;
using FeedService.Abstractions;
using FeedService.Data;
using FeedService.Helpers.KafkaConfiguration;
using FeedService.Models;
using Microsoft.EntityFrameworkCore;
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
            GroupId = "feed-service-user-profile-updated",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.UserServiceTopic);

        _logger.LogInformation(
            "UserProfileUpdatedConsumer started. Group: feed-service-user-profile-updated");

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

        var userSummaryStore = scope.ServiceProvider
            .GetRequiredService<IUserSummaryStore>();
        var db = scope.ServiceProvider
            .GetRequiredService<FeedDbContext>();

        await userSummaryStore.UpsertAsync(new UserSummary
        {
            UserId = @event.UserId,
            DisplayName = @event.FirstName + " " + @event.LastName,
            ProfileImageKey = @event.ProfileImageKey,
            Headline = @event.Headline,
            UpdatedAt = @event.UpdatedAt,
        }, ct);



        var authorAffected = await db.PostSnapshots
            .Where(p => p.AuthorId == @event.UserId && !p.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.AuthorDisplayName, @event.FirstName + " " + @event.LastName)
                .SetProperty(p => p.AuthorProfileImageKey, @event.ProfileImageKey)
                .SetProperty(p => p.AuthorHeadline, @event.Headline)
                .SetProperty(p => p.UpdatedAt, @event.UpdatedAt),
                ct);

        var repostOfAffected = await db.PostSnapshots
            .Where(p => p.RepostOfAuthorId == @event.UserId && !p.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.RepostOfAuthorDisplayName, @event.FirstName + " " + @event.LastName),
                ct);

        _logger.LogInformation(
            "UserProfileUpdated processed: UserId={UserId}, AuthorSnapshotsAffected={A}, RepostOfSnapshotsAffected={R}",
            @event.UserId, authorAffected, repostOfAffected);
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



