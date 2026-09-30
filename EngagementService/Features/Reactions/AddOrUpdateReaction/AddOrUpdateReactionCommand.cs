using System;
using EngagementService.Behaviors;
using EngagementService.Enums;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Reactions.AddOrUpdateReaction;

public record AddOrUpdateReactionCommand(
    TargetType TargetType,
    long TargetId,
    ReactionType Type
) : IRequest<Response<AddOrUpdateReactionResponse>>, ITransactionCommand;
