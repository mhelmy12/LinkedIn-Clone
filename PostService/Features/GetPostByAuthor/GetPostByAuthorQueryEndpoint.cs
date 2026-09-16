using System;
using Carter;
using MediatR;

namespace PostService.Features.GetPostByAuthor;

public class GetPostByAuthorQueryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/posts/author/{authorId}", async (Guid authorId) =>
        {

            return Results.Ok();
        });
    }
}
