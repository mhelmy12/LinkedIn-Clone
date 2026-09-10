using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.CancelConnection;

public class CancelConnectionCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/users/connections/{connectionId}/cancel", async (string connectionId, IMediator mediator) =>
        {
            var command = new CancelConnectionCommand(connectionId);
            var response = await mediator.Send(command);
            return EndpointResponse.Result(response);
        })
        .WithName("CancelConnection")
        .WithTags("Connections");
    }
}
