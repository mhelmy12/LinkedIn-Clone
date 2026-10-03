using System;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Comments.GetCommentReplies;

public record GetCommentRepliesQuery(
    long CommentId,
    string? Cursor,
    int Limit
) : IRequest<Response<GetCommentRepliesResponse>>;