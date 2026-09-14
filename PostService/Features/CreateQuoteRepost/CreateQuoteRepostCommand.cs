using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.CreateQuoteRepost;

public record CreateQuoteRepostCommand() : IRequest<Response<CreateQuoteRepostCommandResponse>>;
