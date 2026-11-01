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
/// Marks removed connections inactive in both directions (FR-001/FR-002).
/// Send is blocked until a fresh <c>ConnectionAccepted</c> re-activates the pair.
/// </summary>
public sealed class ConnectionRemovedConsumer : KafkaConsumerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<ConnectionRemovedConsumer> _logger;

    public ConnectionRemovedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ConnectionRemovedConsumer> logger)
        : base(options, scopeFactory, logger)
    {
        _logger = logger;
    }

    protected override string GroupId => "chat-service-connection-removed";

    protected override string EventType => nameof(ConnectionRemovedEvent);

    protected override async Task HandleEventAsync(
        IServiceProvider services,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<ConnectionRemovedEvent>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException(
                "ConnectionRemoved payload could not be deserialized.");

        if (string.IsNullOrWhiteSpace(@event.UserId1) ||
            string.IsNullOrWhiteSpace(@event.UserId2))
        {
            throw new InvalidOperationException(
                "ConnectionRemoved payload is missing user ids.");
        }

        var db = services.GetRequiredService<ChatDbContext>();

        await ConnectionProjectionWriter.ApplyAsync(
            db,
            @event.UserId1,
            @event.UserId2,
            isActive: false,
            @event.RemovedAt,
            cancellationToken);

        _logger.LogInformation(
            "ConnectionRemoved applied: {User1} <-> {User2} at {RemovedAt}",
            @event.UserId1, @event.UserId2, @event.RemovedAt);
    }
}
