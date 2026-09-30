using System;

namespace EngagementService.Events;

public record ReactionChangedEvent(
    string ReactionId,
    string UserId,
    int TargetType,
    string TargetId,
    int OldType,
    int NewType,
    DateTime UpdatedAt);