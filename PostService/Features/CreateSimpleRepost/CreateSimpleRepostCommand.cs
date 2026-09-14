using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.CreateSimpleRepost;

public record CreateSimpleRepostCommand() : IRequest<Response<CreateSimpleRepostCommandResponse>>;
