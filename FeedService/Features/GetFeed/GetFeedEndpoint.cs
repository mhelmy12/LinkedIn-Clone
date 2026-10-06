using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace FeedService.Features.GetFeed;

public class GetFeedEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/feed", async (
            string? cursor,
            int? limit,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetFeedQuery(
                Cursor: cursor,
                Limit: limit ?? 20);

            var response = await mediator.Send(query, ct);
            return EndpointResponse.Result(response);
        })
        .WithName("GetFeed")
        .WithTags("Feed");
    }
}
