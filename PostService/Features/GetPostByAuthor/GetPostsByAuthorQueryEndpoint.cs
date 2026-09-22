using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.GetPostByAuthor;

public class GetPostsByAuthorQueryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/posts/author/{authorId}", async (
            long authorId,
            string? cursor,
            int? limit,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetPostsByAuthorQuery(
                AuthorId: authorId.ToString(),
                Cursor: cursor,
                Limit: limit ?? 20);

            var response = await mediator.Send(query, ct);
            return EndpointResponse.Result(response);
        })
        .WithName("GetPostsByAuthorId")
        .WithTags("Posts");
    }
}
