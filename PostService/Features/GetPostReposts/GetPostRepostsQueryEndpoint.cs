using System;
using Carter;
using MediatR;

namespace PostService.Features.GetPostReposts;

public class GetPostRepostsQueryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/posts/{postId}/reposts", async (long postId) =>
        {

            return Results.Ok();
        });
    }
}
