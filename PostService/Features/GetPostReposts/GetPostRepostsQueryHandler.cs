using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.GetPostReposts;

public class GetPostRepostsQueryHandler() : ResponseHandler, IRequestHandler<GetPostRepostsQuery, Response<GetPostRepostsQueryResponse>>
{
    public async Task<Response<GetPostRepostsQueryResponse>> Handle(GetPostRepostsQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
