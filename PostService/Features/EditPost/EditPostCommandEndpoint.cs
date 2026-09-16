using System;
using Carter;
using MediatR;

namespace PostService.Features.EditPost;

public class EditPostCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/posts/{postId}", async (long postId) =>
        {

            return Results.Ok();
        });
    }
}
