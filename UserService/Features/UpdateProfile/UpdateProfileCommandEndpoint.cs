using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.UpdateProfile;

public class UpdateProfileCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/users/profile", async (UpdateProfileCommand command, IMediator mediator) =>
        {
            var response = await mediator.Send(command);
            return EndpointResponse.Result(response);
        })
        .WithName("UpdateProfile")
        .WithTags("Profile");
    }
}
