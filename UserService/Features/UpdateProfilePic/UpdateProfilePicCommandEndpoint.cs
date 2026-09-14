using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.UpdateProfilePic;

public class UpdateProfilePicCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/users/profile-picture", async (UpdateProfilePicCommand command, IMediator mediator) =>
        {
            var response = await mediator.Send(command);
            return EndpointResponse.Result(response);
        })
        .WithName("UpdateProfilePic");
    }
}
