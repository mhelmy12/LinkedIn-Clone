using System;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Comments.GetCommentsByPost;

public record GetCommentsByPostQuery(
    long PostId,
    string? Cursor,
    int Limit
) : IRequest<Response<GetCommentsByPostResponse>>;