using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.GetPostByAuthor;

public class GetPostByAuthorQueryHandler(

) : ResponseHandler, IRequestHandler<GetPostByAuthorQuery, Response<GetPostByAuthorQueryResponse>>
{
    public async Task<Response<GetPostByAuthorQueryResponse>> Handle(GetPostByAuthorQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
