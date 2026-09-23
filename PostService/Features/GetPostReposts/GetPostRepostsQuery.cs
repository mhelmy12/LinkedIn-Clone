using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.GetPostReposts;

public record GetPostRepostsQuery(
    string PostId,
    string? Cursor,
    int Limit
    ) : IRequest<Response<GetPostRepostsQueryResponse>>;

