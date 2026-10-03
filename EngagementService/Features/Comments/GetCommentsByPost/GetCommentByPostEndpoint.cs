using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Comments.GetCommentsByPost;

public class GetCommentByPostEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/engagements/comments/post/{id:long}", async (
           long id,
           string? cursor,
           int? limit,
           IMediator mediator,
           CancellationToken ct) =>
       {
           var query = new GetCommentsByPostQuery(
               PostId: id,
               Cursor: cursor,
               Limit: limit ?? 20);

           var response = await mediator.Send(query, ct);
           return EndpointResponse.Result(response);
       })
       .WithName("GetCommentsByPost")
       .WithTags("Comments");
    }
}
