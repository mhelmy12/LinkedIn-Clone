using Carter;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.GetConnectionStatus;

public class GetConnectionStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/users/connections/{otherUserId}/status",
                async (string otherUserId, IMediator mediator) =>
                {
                    var query = new GetConnectionStatusQuery { OtherUserId = otherUserId };
                    var response = await mediator.Send(query);
                    return EndpointResponse.Result(response);
                })
            .WithName("GetConnectionStatus")
            .WithTags("Connections");
    }
}
