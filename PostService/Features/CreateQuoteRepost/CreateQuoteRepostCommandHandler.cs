using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.CreateQuoteRepost;

public class CreateQuoteRepostCommandHandler(

) : ResponseHandler, IRequestHandler<CreateQuoteRepostCommand, Response<CreateQuoteRepostCommandResponse>>
{
    public async Task<Response<CreateQuoteRepostCommandResponse>> Handle(CreateQuoteRepostCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
