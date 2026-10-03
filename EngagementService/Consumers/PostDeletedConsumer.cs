using System;
using System.Text.Json;
using Confluent.Kafka;
using EngagementService.Data;
using EngagementService.Enums;
using EngagementService.Helpers.KafkaConfiguration;
using Microsoft.EntityFrameworkCore;

namespace EngagementService.Consumers;


public record PostDeletedEvent(
    string PostId,
    string AuthorId,
    DateTimeOffset DeletedAt);
public class PostDeletedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<PostDeletedConsumer> _logger;

    public PostDeletedConsumer(
        IConsumer<string, string> consumer,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<PostDeletedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
        _consumer = KafkaConsumerFactory.Create(
           config.GetValue<string>("Kafka:BootstrapServers")!,
           "post-deleted-consumer-group"

       );
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("postService.LinkedInPostDb.dbo.OutboxMessages");

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = _consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (result?.Message?.Value is null)
                continue;

            try
            {
                await ProcessAsync(result.Message.Value, stoppingToken);
                _consumer.Commit(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PostDeleted event");
            }
        }

        _consumer.Close();
    }

    private async Task ProcessAsync(string rawJson, CancellationToken ct)
    {
        // 1. Parse Debezium envelope
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("after", out var after) || after.ValueKind == JsonValueKind.Null)
            return;

        var type = after.GetProperty("Type").GetString();
        if (type != "PostDeleted")
            return;

        var payloadJson = after.GetProperty("Payload").GetString();
        if (string.IsNullOrWhiteSpace(payloadJson))
            return;

        var @event = JsonSerializer.Deserialize<PostDeletedEvent>(
            payloadJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (@event is null)
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EngagmentDbContext>();

        var postId = long.Parse(@event.PostId);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Soft delete Reactions
        await db.Reactions
            .Where(r => r.TargetType == TargetType.Post
                     && r.TargetId == postId
                     && !r.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsDeleted, true), ct);

        // Soft delete Comments
        await db.Comments
            .Where(c => c.PostId == postId && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true), ct);

        await tx.CommitAsync(ct);

        _logger.LogInformation(
            "Cleaned up engagement for deleted post {PostId}", postId);
    }
}
