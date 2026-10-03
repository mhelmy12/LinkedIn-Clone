using System;
using Carter;
using EngagementService.Enums;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Reactions.GetUserReaction;

public class GetUserReactionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/engagements/reactions/me", async (
          TargetType targetType,
          long targetId,
          IMediator mediator,
          CancellationToken ct) =>
      {
          var response = await mediator.Send(
              new GetUserReactionQuery(targetType, targetId), ct);

          return EndpointResponse.Result(response);
      })
      .WithName("GetUserReaction")
      .WithTags("Reactions");
    }
}
