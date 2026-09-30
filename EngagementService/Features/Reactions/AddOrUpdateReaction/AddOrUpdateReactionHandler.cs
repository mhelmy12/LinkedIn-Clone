using System;
using System.Text.Json;
using EngagementService.Data;
using EngagementService.Events;
using EngagementService.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using Shared.Services.CurrentUserService;
using Shared.Services.IdGeneratorService;

namespace EngagementService.Features.Reactions.AddOrUpdateReaction;

public class AddOrUpdateReactionHandler(
    EngagmentDbContext _dbContext,
    [FromKeyedServices("Snowflake")] IIdGeneratorService _snowflakeIdGenerator,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
    ILogger<AddOrUpdateReactionHandler> _logger

) : ResponseHandler, IRequestHandler<AddOrUpdateReactionCommand, Response<AddOrUpdateReactionResponse>>
{
    public async Task<Response<AddOrUpdateReactionResponse>> Handle(AddOrUpdateReactionCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized<AddOrUpdateReactionResponse>("User is not authenticated");

        var now = DateTime.UtcNow;

        var existing = await _dbContext.Reactions
            .FirstOrDefaultAsync(r =>
                r.UserId == currentUserId
                && r.TargetType == request.TargetType
                && r.TargetId == request.TargetId,
                cancellationToken);


        if (existing is not null)
        {
            // no-op
            if (existing.Type == request.Type)
            {
                _logger.LogDebug(
                    "Reaction {ReactionId} already has type {Type} — no-op",
                    existing.Id, request.Type);

                return Success(MapResponse(existing, isNew: false, changed: false),
                    "Reaction already exists with the same type.");
            }

            //UPDATE
            var oldType = existing.Type;
            existing.Type = request.Type;
            existing.UpdatedAt = now;

            var changedEvent = new ReactionChangedEvent(
                ReactionId: existing.Id.ToString(),
                UserId: currentUserId,
                TargetType: (int)existing.TargetType,
                TargetId: existing.TargetId.ToString(),
                OldType: (int)oldType,
                NewType: (int)request.Type,
                UpdatedAt: now);


            _dbContext.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Payload = JsonSerializer.Serialize(changedEvent),
                Type = nameof(ReactionChangedEvent),
                AggregateType = nameof(Reaction),
                AggregateId = existing.Id.ToString(),
                Timestamp = now
            });




            _logger.LogInformation(
                "Reaction {ReactionId} changed from {Old} to {New}",
                existing.Id, oldType, request.Type);

            return Success(MapResponse(existing, isNew: false, changed: true),
                "Reaction updated successfully.");
        }

        //  INSERT
        var newReaction = new Reaction
        {
            Id = long.Parse(_snowflakeIdGenerator.Generate()),
            UserId = currentUserId,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            Type = request.Type,
            CreatedAt = now,
            IsDeleted = false
        };

        _dbContext.Reactions.Add(newReaction);

        var addedEvent = new ReactionAddedEvent(
            ReactionId: newReaction.Id.ToString(),
            UserId: currentUserId,
            TargetType: (int)newReaction.TargetType,
            TargetId: newReaction.TargetId.ToString(),
            ReactionType: (int)newReaction.Type,
            CreatedAt: now);

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Payload = JsonSerializer.Serialize(addedEvent),
            Type = nameof(ReactionAddedEvent),
            AggregateType = nameof(Reaction),
            AggregateId = newReaction.Id.ToString(),
            Timestamp = now
        });


        _logger.LogInformation(
            "Reaction {ReactionId} added by {UserId} on {TargetType}:{TargetId}",
            newReaction.Id, currentUserId, request.TargetType, request.TargetId);

        return Success(MapResponse(newReaction, isNew: true, changed: false),
            "Reaction added successfully.");
    }


    private static AddOrUpdateReactionResponse MapResponse(
        Reaction r, bool isNew, bool changed)
    {
        return new AddOrUpdateReactionResponse(
            ReactionId: r.Id.ToString(),
            TargetType: (int)r.TargetType,
            TargetId: r.TargetId.ToString(),
            Type: (int)r.Type,
            CreatedAt: r.CreatedAt,
            UpdatedAt: r.UpdatedAt,
            IsNew: isNew,
            Changed: changed);
    }
}
