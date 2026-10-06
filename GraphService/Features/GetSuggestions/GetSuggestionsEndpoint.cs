using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace GraphService.Features.GetSuggestions;

public class GetSuggestionsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/graph/suggestions", async (
           int? limit,
           IMediator mediator,
           CancellationToken ct) =>
       {
           var query = new GetSuggestionsQuery(Limit: limit ?? 10);
           var response = await mediator.Send(query, ct);
           return EndpointResponse.Result(response);
       })
       .WithName("GetSuggestions")
       .WithTags("Graph");
    }
}
