using System;
using MediatR;
using PostService.Behaviors;
using Shared.Helpers;

namespace PostService.Features.Unrepost;

public record UnrepostCommand(long OriginalPostId) : IRequest<Response<UnrepostCommandResponse>>, ITransactionCommand;
