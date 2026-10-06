using System;

namespace GraphService.Features.GetMutualConnections;

using GraphService.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Helpers;

public class GetMutualConnectionsQueryHandler(
    IGraphRepository _graphRepository,
    ILogger<GetMutualConnectionsQueryHandler> _logger
) : ResponseHandler, IRequestHandler<GetMutualConnectionsQuery, Response<GetMutualConnectionsResponse>>
{
    public async Task<Response<GetMutualConnectionsResponse>> Handle(
        GetMutualConnectionsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await _graphRepository.GetMutualConnectionsAsync(
            request.UserId1,
            request.UserId2,
            request.Limit,
            cancellationToken);

        var items = results
            .Select(r => new MutualConnectionDto(
                UserId: r.UserId,
                DisplayName: r.DisplayName,
                ProfileImageKey: r.ProfileImageKey,
                Headline: r.Headline))
            .ToList();

        _logger.LogDebug(
            "Found {Count} mutual connections between {User1} and {User2}",
            items.Count, request.UserId1, request.UserId2);

        return Success(new GetMutualConnectionsResponse(items));
    }
}