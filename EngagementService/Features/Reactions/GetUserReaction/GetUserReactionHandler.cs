using System;
using EngagementService.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using Shared.Services.CurrentUserService;

namespace EngagementService.Features.Reactions.GetUserReaction;

public class GetUserReactionHandler(
    EngagmentDbContext _dbContext,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
    ILogger<GetUserReactionHandler> _logger
) : ResponseHandler, IRequestHandler<GetUserReactionQuery, Response<GetUserReactionResponse>>
{
    public async Task<Response<GetUserReactionResponse>> Handle(
        GetUserReactionQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();


        if (currentUserId is null)
            return Unauthorized<GetUserReactionResponse>("User is not authenticated");

        _logger.LogInformation($"Getting user reaction for user {currentUserId} on target {request.TargetType} with id {request.TargetId}");
        var reaction = await _dbContext.Reactions
            .AsNoTracking()
            .Where(r => r.UserId == currentUserId)
            .Where(r => r.TargetType == request.TargetType)
            .Where(r => r.TargetId == request.TargetId)
            .Select(r => new
            {
                r.Id,
                r.Type,
                r.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (reaction is null)
        {
            return Success(new GetUserReactionResponse(
                HasReacted: false,
                ReactionId: null,
                Type: null,
                CreatedAt: null));
        }

        return Success(new GetUserReactionResponse(
            HasReacted: true,
            ReactionId: reaction.Id,
            Type: (int)reaction.Type,
            CreatedAt: reaction.CreatedAt));
    }
}
