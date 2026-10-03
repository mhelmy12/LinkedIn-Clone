using System;

namespace EngagementService.Features.Reactions.GetUserReaction;

public record GetUserReactionResponse(
    bool HasReacted,
    long? ReactionId,
    int? Type,
    DateTime? CreatedAt);