using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace GraphService.Features.GetMutualConnections;

public class GetMutualConnectionsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/graph/mutual/{userId1}/{userId2}", async (
           string userId1,
           string userId2,
           int? limit,
           IMediator mediator,
           CancellationToken ct) =>
       {
           var query = new GetMutualConnectionsQuery(
               UserId1: userId1,
               UserId2: userId2,
               Limit: limit ?? 20);

           var response = await mediator.Send(query, ct);
           return EndpointResponse.Result(response);
       })
       .WithName("GetMutualConnections")
       .WithTags("Graph");
    }
}
