using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.DeleteSimpleRepost;

public record DeleteSimpleRepostCommand() : IRequest<Response<DeleteSimpleRepostCommandResponse>>;
