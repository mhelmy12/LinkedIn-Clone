using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.DeletePost;

public class DeletePostCommandHandler(

) : ResponseHandler, IRequestHandler<DeletePostCommand, Response<DeletePostCommandResponse>>
{
    public async Task<Response<DeletePostCommandResponse>> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
