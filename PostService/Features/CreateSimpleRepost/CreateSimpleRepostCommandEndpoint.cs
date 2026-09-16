using System;
using Carter;
using MediatR;

namespace PostService.Features.CreateSimpleRepost;

public class CreateSimpleRepostCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/posts/{postId}/reposts", async (long postId) =>
        {
            return Results.Ok();
        });
    }
}
