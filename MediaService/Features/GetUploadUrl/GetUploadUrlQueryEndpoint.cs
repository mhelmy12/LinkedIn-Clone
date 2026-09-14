using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace MediaService.Features.GetUploadUrl;

public class GetUploadUrlQueryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/media/upload-url", async (IMediator mediator) =>
        {
            var query = new GetUploadUrlQuery();
            var response = await mediator.Send(query);
            return EndpointResponse.Result(response);
        })
        .WithName("GetUploadUrl")
        .WithTags("User");

    }
}
