using System;

namespace EngagementService.Events;

public record ReactionRemovedEvent(
    long ReactionId,
    string UserId,
    int TargetType,
    long TargetId,
    DateTime RemovedAt);