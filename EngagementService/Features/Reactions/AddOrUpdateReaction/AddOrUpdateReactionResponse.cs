using System;

namespace EngagementService.Features.Reactions.AddOrUpdateReaction;

public record AddOrUpdateReactionResponse(
    string ReactionId,
    int TargetType,
    string TargetId,
    int Type,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsNew,
    bool Changed);