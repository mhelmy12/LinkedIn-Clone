using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.EditPost;

public record EditPostCommand() : IRequest<Response<EditPostCommandResponse>>;
