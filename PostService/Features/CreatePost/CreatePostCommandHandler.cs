using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.CreatePost;

public class CreatePostCommandHandler(

) : ResponseHandler, IRequestHandler<CreatePostCommand, Response<CreatePostCommandResponse>>
{
    public async Task<Response<CreatePostCommandResponse>> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
