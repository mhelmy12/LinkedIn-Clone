using System;
using Carter;
using MediatR;

namespace PostService.Features.Unrepost;

public class UnrepostCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/posts/{postId}/reposts", async (long postId) =>
        {
            return Results.Ok();
        });
    }
}
