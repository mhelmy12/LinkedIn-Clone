using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.GetPostById;

public class GetPostByIdQueryHandler(

) : ResponseHandler, IRequestHandler<GetPostByIdQuery, Response<GetPostByIdQueryResponse>>
{
    public async Task<Response<GetPostByIdQueryResponse>> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
