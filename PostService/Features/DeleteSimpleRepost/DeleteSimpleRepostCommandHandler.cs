using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.DeleteSimpleRepost;

public class DeleteSimpleRepostCommandHandler(

) : ResponseHandler, IRequestHandler<DeleteSimpleRepostCommand, Response<DeleteSimpleRepostCommandResponse>>
{
    public async Task<Response<DeleteSimpleRepostCommandResponse>> Handle(DeleteSimpleRepostCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
