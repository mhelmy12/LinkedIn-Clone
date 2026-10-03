using System;
using EngagementService.Behaviors;
using MediatR;
using Shared.Helpers;

namespace EngagementService.Features.Comments.DeleteComment;

public record DeleteCommentCommand(long CommentId) : IRequest<Response<DeleteCommentResponse>> , ITransactionCommand;
