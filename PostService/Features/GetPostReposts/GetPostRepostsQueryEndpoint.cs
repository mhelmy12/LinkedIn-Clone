using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.GetPostReposts;

public class GetPostRepostsQueryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/posts/{postId}/reposts", async (long postId, string? cursor, int? limit, IMediator mediator, CancellationToken ct) =>
        {
            var query = new GetPostRepostsQuery(
               PostId: postId.ToString(),
               Cursor: cursor,
               Limit: limit ?? 20);

            var response = await mediator.Send(query, ct);
            return EndpointResponse.Result(response);

        });
    }
}
