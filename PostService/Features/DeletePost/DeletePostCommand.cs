using System;
using MediatR;
using PostService.Behaviors;
using Shared.Helpers;

namespace PostService.Features.DeletePost;

public record DeletePostCommand(
    string PostId
) : IRequest<Response<DeletePostCommandResponse>>, ITransactionCommand;
