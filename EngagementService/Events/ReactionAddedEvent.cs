using System;
using StackExchange.Redis;

namespace EngagementService.Events;

public record ReactionAddedEvent(
    string ReactionId,
    string UserId,
    int TargetType,
    string TargetId,
    int ReactionType,
    DateTime CreatedAt);