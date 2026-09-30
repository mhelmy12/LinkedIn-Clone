using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Reactions.AddOrUpdateReaction;

public class AddOrUpdateReactionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/engagements/reactions", async (
          AddOrUpdateReactionCommand command,
          IMediator mediator,
          CancellationToken ct) =>
      {
          var response = await mediator.Send(command, ct);
          return EndpointResponse.Result(response);
      })
      .WithName("AddOrUpdateReaction")
      .WithTags("Reactions");

    }
}
