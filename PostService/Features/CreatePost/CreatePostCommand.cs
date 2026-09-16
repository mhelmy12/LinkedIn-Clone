using System;
using System.Text.Json.Serialization;
using MediatR;
using PostService.Behaviors;
using PostService.Models;
using Shared.Helpers;

namespace PostService.Features.CreatePost;

//For Quoted Posts and Normal Posts
public record MediaItemDto(string MediaKey, MediaType Type, int DisplayOrder);
public record CreatePostCommand(
    string? Content,
    VisibilityType Visibility,
    long? QuotedPostId,
    List<MediaInput>? Media,
    List<MentionInput>? Mentions) : IRequest<Response<CreatePostCommandResponse>>, ITransactionCommand;

public record MediaInput(
    string MediaKey,
    MediaType MediaType,
    int DisplayOrder);

public record MentionInput(
    string MentionedUserId,
    int StartIndex,
    int Length);