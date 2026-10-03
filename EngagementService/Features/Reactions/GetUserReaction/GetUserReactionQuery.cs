using System;
using EngagementService.Enums;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Reactions.GetUserReaction;

public record GetUserReactionQuery(
    TargetType TargetType,
    long TargetId
) : IRequest<Response<GetUserReactionResponse>>;