using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.DeletePost;

public record DeletePostCommand() : IRequest<Response<DeletePostCommandResponse>>;
