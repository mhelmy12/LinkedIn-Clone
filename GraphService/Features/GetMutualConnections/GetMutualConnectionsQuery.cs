using System;
using MediatR;
using Shared.Helpers;

namespace GraphService.Features.GetMutualConnections;

public record GetMutualConnectionsQuery(
    string UserId1,
    string UserId2,
    int Limit
) : IRequest<Response<GetMutualConnectionsResponse>>;
