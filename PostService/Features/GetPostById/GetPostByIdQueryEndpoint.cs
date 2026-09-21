using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.GetPostById;

public class GetPostByIdQueryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/posts/{postId:long}", async (long postId, IMediator mediator) =>
        {
            var response = await mediator.Send(new GetPostByIdQuery(postId));
            return EndpointResponse.Result(response);
        })
        .WithName("GetPostById")
        .WithTags("Posts");
    }
}
