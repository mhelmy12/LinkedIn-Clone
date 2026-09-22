using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.GetPostByAuthor;

public class GetPostsByAuthorQueryHandler(

) : ResponseHandler, IRequestHandler<GetPostsByAuthorQuery, Response<GetPostsByAuthorQueryResponse>>
{
    public async Task<Response<GetPostsByAuthorQueryResponse>> Handle(GetPostsByAuthorQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
