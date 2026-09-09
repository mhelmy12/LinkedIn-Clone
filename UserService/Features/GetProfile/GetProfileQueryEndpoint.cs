using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.GetProfile;

public class GetProfileQueryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/users/profile/{userId}", async (string userId, IMediator mediator) =>
        {
            var query = new GetProfileQuery { UserId = userId };
            var response = await mediator.Send(query);
            return EndpointResponse.Result(response);
        });
    }
}
