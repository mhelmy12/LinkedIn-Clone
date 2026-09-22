using System;
using MediatR;
using Shared.Helpers;

namespace PostService.Features.GetPostByAuthor;


public record GetPostsByAuthorQuery(
    long AuthorId,
    string? Cursor,
    int Limit) : IRequest<Response<GetPostsByAuthorQueryResponse>>;
