using System;
using Carter;
using MediatR;
using Shared.Helpers;

namespace SearchService.Features.SearchUsers;

public class SearchUsersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/search/users", async (IMediator mediator, string query, int page = 1, int pageSize = 10) =>
        {
            var command = new SearchUsersQuery(query, page, pageSize);
            
            var response = await mediator.Send(command);
            
            
            return EndpointResponse.Result(response);
        })
        .WithName("SearchUsers");
    }
}
