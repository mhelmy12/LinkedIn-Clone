using System;
using Carter;
using MediatR;

namespace PostService.Features.GetPostById;

public class GetPostByIdQueryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/posts/{postId}", async (long postId) =>
        {

            return Results.Ok();
        });
    }
}
