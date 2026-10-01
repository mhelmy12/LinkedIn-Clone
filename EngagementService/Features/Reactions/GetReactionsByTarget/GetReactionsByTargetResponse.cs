using System;

namespace EngagementService.Features.Reactions.GetReactionsByTarget;

public record GetReactionsByTargetResponse(
    IReadOnlyList<ReactionItemDto> Items,
    string? NextCursor,
    bool HasMore);

public record ReactionItemDto(
    string ReactionId,
    string UserId,
    int Type,
    DateTime CreatedAt);