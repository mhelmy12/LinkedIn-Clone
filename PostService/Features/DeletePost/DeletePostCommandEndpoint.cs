using System;
using Carter;
using MediatR;

namespace PostService.Features.DeletePost;

public class DeletePostCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/posts/{postId}", async (long postId) =>
        {
            return Results.Ok();
        });
    }
}
