using System;
using Carter;
using MediatR;

namespace PostService.Features.DeleteSimpleRepost;

public class DeleteSimpleRepostCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/posts/{postId}/reposts", async (long postId) =>
        {
            return Results.Ok();
        });
    }
}
