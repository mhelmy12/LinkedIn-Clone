using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Comments.DeleteComment;

public class DeleteCommentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/engagements/comments/{id:long}", async (
        long id,
        IMediator mediator,
        CancellationToken ct) =>
    {
        var response = await mediator.Send(
            new DeleteCommentCommand(id), ct);

        return EndpointResponse.Result(response);
    })
    .WithName("DeleteComment")
    .WithTags("Comments");
    }
}
