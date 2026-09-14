using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.CreatePost;

public record CreatePostCommand() : IRequest<Response<CreatePostCommandResponse>>;