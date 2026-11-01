using System.Text.Json;
using ChatService.Data;
using ChatService.Events;
using ChatService.Helpers;
using ChatService.Helpers.KafkaConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatService.Consumers;

/// <summary>
/// Mirrors accepted connections into <c>ChatConnectionProjection</c> for both
/// directions (FR-001). Redelivery is a no-op because the writer upserts.
/// </summary>
public sealed class ConnectionAcceptedConsumer : KafkaConsumerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<ConnectionAcceptedConsumer> _logger;

    public ConnectionAcceptedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ConnectionAcceptedConsumer> logger)
        : base(options, scopeFactory, logger)
    {
        _logger = logger;
    }

    protected override string GroupId => "chat-service-connection-accepted";

    protected override string EventType => nameof(ConnectionAcceptedEvent);

    protected override async Task HandleEventAsync(
        IServiceProvider services,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<ConnectionAcceptedEvent>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException(
                "ConnectionAccepted payload could not be deserialized.");

        if (string.IsNullOrWhiteSpace(@event.UserId1) ||
            string.IsNullOrWhiteSpace(@event.UserId2))
        {
            throw new InvalidOperationException(
                "ConnectionAccepted payload is missing user ids.");
        }

        var db = services.GetRequiredService<ChatDbContext>();

        await ConnectionProjectionWriter.ApplyAsync(
            db,
            @event.UserId1,
            @event.UserId2,
            isActive: true,
            @event.AcceptedAt,
            cancellationToken);

        _logger.LogInformation(
            "ConnectionAccepted applied: {User1} <-> {User2} at {AcceptedAt}",
            @event.UserId1, @event.UserId2, @event.AcceptedAt);
    }
}
