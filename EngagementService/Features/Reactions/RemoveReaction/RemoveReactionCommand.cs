using System;
using EngagementService.Behaviors;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Reactions.RemoveReaction;

public record RemoveReactionCommand(
    int TargetType,
    long TargetId

) : IRequest<Response<RemoveReactionResponse>>, ITransactionCommand;