using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.RejectConnection;

public class RejectConnectionCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/connections/{connectionId}/reject", async (string connectionId, IMediator mediator) =>
        {
            var request = new RejectConnectionCommand(connectionId);
            var response = await mediator.Send(request);
            return EndpointResponse.Result(response);
        })
        .WithName("RejectConnection")
        .WithTags("Connections");
    }
}
