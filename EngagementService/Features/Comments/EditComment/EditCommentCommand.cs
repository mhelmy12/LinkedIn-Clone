using System;
using EngagementService.Behaviors;
using EngagementService.Models;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Comments.EditComment;

public record EditCommentCommand(
    long CommentId,
    string? Content,
    List<CommentMediaInput>? Media,
    List<CommentMentionInput>? Mentions
) : IRequest<Response<EditCommentResponse>>, ITransactionCommand;


public record CommentMediaInput(
    string ObjectKey,
    MediaType MediaType,
    int DisplayOrder);


public record CommentMentionInput(
string MentionedUserId,
int StartIndex,
int Length);
