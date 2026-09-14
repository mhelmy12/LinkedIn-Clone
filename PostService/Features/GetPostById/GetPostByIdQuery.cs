using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.GetPostById;

public record GetPostByIdQuery() : IRequest<Response<GetPostByIdQueryResponse>>;
