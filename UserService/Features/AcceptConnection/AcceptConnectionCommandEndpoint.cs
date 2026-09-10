using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.AcceptConnection;

public class AcceptConnectionCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/users/connections/{connectionId}/accept", async (string connectionId, IMediator mediator) =>
        {
            var command = new AcceptConnectionCommand(connectionId);
            var response = await mediator.Send(command);
            return EndpointResponse.Result(response);
        })
        .WithName("AcceptConnection")
        .WithTags("Connections");

    }
}
