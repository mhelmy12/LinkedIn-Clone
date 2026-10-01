using System;
using Carter;
using EngagementService.Enums;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Reactions.GetReactionsByTarget;

public class GetReactionsByTargetEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/engagements/reactions/target/{targetType}/{targetId:long}", async (
           TargetType targetType,
           long targetId,
           ReactionType? type,
           string? cursor,
           int? limit,
           IMediator mediator,
           CancellationToken ct) =>
       {
           var query = new GetReactionsByTargetQuery(
               TargetType: targetType,
               TargetId: targetId,
               Type: type,
               Cursor: cursor,
               Limit: limit ?? 20);

           var response = await mediator.Send(query, ct);
           return EndpointResponse.Result(response);
       })
        .WithName("GetReactionsByTarget")
        .WithTags("Reactions")
        .Produces(200);
    }
}
