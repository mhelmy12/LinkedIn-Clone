using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.CreatePost;

public class CreatePostCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/posts", async (CreatePostCommand request, IMediator mediator) =>
        {
            var result = await mediator.Send(request);
            return EndpointResponse.Result(result);
        })
        .WithName("CreatePost")
        .WithTags("Posts");
    }
}
