using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Reactions.RemoveReaction;

public class RemoveReactionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/engagements/reactions", async (
          int targetType,
          long targetId,
          IMediator mediator,
          CancellationToken ct) =>
      {
          var response = await mediator.Send(
              new RemoveReactionCommand(targetType, targetId), ct);

          return EndpointResponse.Result(response);
      })
      .WithName("RemoveReaction")
      .WithTags("Reactions")
      .Produces(StatusCodes.Status204NoContent)
      .Produces(StatusCodes.Status400BadRequest)
      .Produces(StatusCodes.Status401Unauthorized);
    }
}
