using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.RemoveConnection;

public class RemoveConnectionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/users/connections/{targetUserId}", async (
            string targetUserId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new RemoveConnectionCommand(targetUserId);
            var response = await mediator.Send(command, ct);

            return EndpointResponse.Result(response);
        })
        .WithName("RemoveConnection")
        .WithTags("Connections");
    }
}
