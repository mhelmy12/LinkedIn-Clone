using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.EditPost;

public class EditPostCommandHandler(

) : ResponseHandler, IRequestHandler<EditPostCommand, Response<EditPostCommandResponse>>
{
    public async Task<Response<EditPostCommandResponse>> Handle(EditPostCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
