using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.GetPostReposts;

public record GetPostRepostsQuery() : IRequest<Response<GetPostRepostsQueryResponse>>;

