using System;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Helpers;

namespace PostService.Features.EditPost;

public class EditPostCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/posts/{postId}", async ([FromRoute] long postId, [FromBody] EditPostCommand command, IMediator mediator) =>
        {
            var response = await mediator.Send(command);
            return EndpointResponse.Result(response);
        });
    }
}
