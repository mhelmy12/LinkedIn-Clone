using System;
using MediatR;
using Shared.Helpers;

namespace SearchService.Features.SearchPosts;

public record SearchPostsQuery(
    string? Query,
    string? AuthorId,
    string? Hashtag,
    string? Cursor,
    int Limit
) : IRequest<Response<SearchPostsResponse>>;
