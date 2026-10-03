using System;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Helpers;

namespace EngagementService.Features.Comments.EditComment;

public class EditCommentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/engagements/comments/{id:long}", async (
            long commentId,
            [FromBody] EditCommentCommand command,
            IMediator sender,
            CancellationToken ct) =>
        {

            var response = await sender.Send(command, ct);
            return EndpointResponse.Result(response);
        })
        .WithName("EditComment")
        .WithTags("Comments");
    }
}
