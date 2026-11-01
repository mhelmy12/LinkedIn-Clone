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
/// Keeps <c>ChatUserProjection</c> display name and email current for
/// conversation headers and offline notifications (FR-013/FR-014).
/// </summary>
public sealed class UserProfileUpdatedConsumer : KafkaConsumerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<UserProfileUpdatedConsumer> _logger;

    public UserProfileUpdatedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<UserProfileUpdatedConsumer> logger)
        : base(options, scopeFactory, logger)
    {
        _logger = logger;
    }

    protected override string GroupId => "chat-service-user-profile-updated";

    protected override string EventType => nameof(UserProfileUpdatedEvent);

    protected override async Task HandleEventAsync(
        IServiceProvider services,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UserProfileUpdatedEvent>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException(
                "UserProfileUpdated payload could not be deserialized.");

        if (string.IsNullOrWhiteSpace(@event.UserId))
        {
            throw new InvalidOperationException(
                "UserProfileUpdated payload is missing the user id.");
        }

        var displayName = $"{@event.FirstName} {@event.LastName}".Trim();
        if (displayName.Length == 0)
        {
            displayName = @event.Email ?? @event.UserId;
        }

        var db = services.GetRequiredService<ChatDbContext>();

        await UserProjectionWriter.ApplyAsync(
            db,
            @event.UserId,
            displayName,
            @event.Email,
            @event.UpdatedAt,
            cancellationToken);

        _logger.LogInformation(
            "UserProfileUpdated applied: UserId={UserId} DisplayName={DisplayName}",
            @event.UserId, displayName);
    }
}
