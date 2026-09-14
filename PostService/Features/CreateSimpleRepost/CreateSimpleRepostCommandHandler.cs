using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.CreateSimpleRepost;

public class CreateSimpleRepostCommandHandler(

) : ResponseHandler, IRequestHandler<CreateSimpleRepostCommand, Response<CreateSimpleRepostCommandResponse>>
{
    public async Task<Response<CreateSimpleRepostCommandResponse>> Handle(CreateSimpleRepostCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
