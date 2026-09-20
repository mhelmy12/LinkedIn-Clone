using System;
using MediatR;
using PostService.Behaviors;
using PostService.Features.CreatePost;
using PostService.Models;
using Shared.Helpers;

namespace PostService.Features.EditPost;

public record EditPostCommand(
    string PostId,
    string? Content,
    VisibilityType? Visibility,
    List<MediaInput>? Media,
    List<MentionInput>? Mentions,
    string RowVersion) : IRequest<Response<EditPostCommandResponse>> , ITransactionCommand;
