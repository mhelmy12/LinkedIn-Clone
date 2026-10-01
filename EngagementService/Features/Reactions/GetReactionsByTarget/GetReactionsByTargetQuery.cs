using System;
using EngagementService.Enums;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Reactions.GetReactionsByTarget;

public record GetReactionsByTargetQuery(
    TargetType TargetType,
    long TargetId,
    ReactionType? Type,
    string? Cursor,
    int Limit
) : IRequest<Response<GetReactionsByTargetResponse>>;