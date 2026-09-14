using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.UpdateCoverPic;

public class UpdateCoverPicCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/users/cover-picture", async (UpdateCoverPicCommand command, IMediator mediator) =>
        {
            var response = await mediator.Send(command);
            return EndpointResponse.Result(response);
        })
        .WithName("UpdateCoverPic");
    }
}
