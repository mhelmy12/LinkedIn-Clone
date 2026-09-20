using System;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Helpers;

namespace PostService.Features.DeletePost;

public class DeletePostCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/posts/{postId}", async (long postId, IMediator mediator) =>
        {
            var response = await mediator.Send(new DeletePostCommand(postId.ToString()));

            return EndpointResponse.Result(response);
        });
    }
}
