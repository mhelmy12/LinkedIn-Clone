using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.SendConnection;

public class SendConnectionCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("users/connections/{targetUserId}/connect", async (string targetUserId, IMediator mediator) =>
        {
            var command = new SendConnectionCommand(targetUserId);
            var response = await mediator.Send(command);
            return EndpointResponse.Result(response);
        })

        .WithName("SendConnection")
        .WithTags("Connections");
    }
}
