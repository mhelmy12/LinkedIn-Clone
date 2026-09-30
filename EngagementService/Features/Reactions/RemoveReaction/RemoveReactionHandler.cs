using System;
using EngagementService.Data;
using EngagementService.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using Shared.Services.CurrentUserService;

namespace EngagementService.Features.Reactions.RemoveReaction;

public class RemoveReactionHandler(
    EngagmentDbContext _dbContext,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
    ILogger<RemoveReactionHandler> _logger
) : ResponseHandler, IRequestHandler<RemoveReactionCommand, Response<RemoveReactionResponse>>
{
    public async Task<Response<RemoveReactionResponse>> Handle(
        RemoveReactionCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized<RemoveReactionResponse>("User is not authenticated");

        var reaction = await _dbContext.Reactions
            .AsNoTracking()
            .Where(r => r.UserId == currentUserId)
            .Where(r => (int)r.TargetType == request.TargetType)
            .Where(r => r.TargetId == request.TargetId)
            .Select(r => new { r.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (reaction is null)
        {
            _logger.LogDebug(
                "No active reaction found for user {UserId} on {TargetType}:{TargetId} — returning 204",
                currentUserId, request.TargetType, request.TargetId);

            return NotFound<RemoveReactionResponse>("No active reaction to remove.");
        }

        var now = DateTime.UtcNow;

        var affected = await _dbContext.Reactions
            .Where(r => r.Id == reaction.Id && !r.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsDeleted, true)
                .SetProperty(r => r.UpdatedAt, now),
                cancellationToken);

        if (affected == 0)
        {
            _logger.LogDebug(
                "Reaction {ReactionId} was removed concurrently — returning 204",
                reaction.Id);

            return NotFound<RemoveReactionResponse>("Reaction already removed.");
        }


        var @event = new ReactionRemovedEvent(
            ReactionId: reaction.Id,
            UserId: currentUserId,
            TargetType: request.TargetType,
            TargetId: request.TargetId,
            RemovedAt: now);



        _dbContext.OutboxMessages.Add(new Models.OutboxMessage
        {
            Id = Guid.NewGuid(),
            Payload = System.Text.Json.JsonSerializer.Serialize(@event),
            Type = nameof(ReactionRemovedEvent),
            AggregateType = nameof(Models.Reaction),
            AggregateId = reaction.Id.ToString(),
            Timestamp = now
        });


        _logger.LogInformation(
            "Reaction {ReactionId} removed by {UserId} from {TargetType}:{TargetId}",
            reaction.Id, currentUserId, request.TargetType, request.TargetId);

        return Success<RemoveReactionResponse>(new(), "Reaction removed successfully.");
    }
}
