using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace SearchService.Features.SearchPosts;

public class SearchPostsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/search/posts", async (
           string? q,
           string? authorId,
           string? hashtag,
           string? cursor,
           int? limit,
           IMediator mediator,
           CancellationToken ct) =>
       {
           var query = new SearchPostsQuery(
               Query: q,
               AuthorId: authorId,
               Hashtag: hashtag,
               Cursor: cursor,
               Limit: limit ?? 20);

           var response = await mediator.Send(query, ct);
           return EndpointResponse.Result(response);
       })
       .RequireAuthorization()
       .WithName("SearchPosts")
       .WithTags("Search");
    }
}
