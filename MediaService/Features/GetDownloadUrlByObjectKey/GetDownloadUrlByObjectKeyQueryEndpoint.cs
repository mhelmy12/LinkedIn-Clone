using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace MediaService.Features.GetDownloadUrlByObjectKey;

public class GetDownloadUrlByObjectKeyQueryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/media/download-url", async (string objectKey, IMediator mediator) =>
        {
            var query = new GetDownloadUrlByObjectKeyQuery(objectKey);
            var response = await mediator.Send(query);
            return EndpointResponse.Result(response);
        })
        .WithDescription("Get a presigned download URL for a specific object key in S3.")
        .WithName("GetDownloadUrlByObjectKey");
    }
}
