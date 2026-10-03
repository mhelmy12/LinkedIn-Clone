using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Comments.CreateComment;

public class CreateCommentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/engagements/comments", async (
           CreateCommentCommand command,
           IMediator mediator,
           CancellationToken ct) =>
       {
           var response = await mediator.Send(command, ct);
           return EndpointResponse.Result(response);
       })
       .WithName("CreateComment")
       .WithTags("Comments");
    }
}
