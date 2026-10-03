using System;
using EngagementService.Models;

namespace EngagementService.Features.Comments.GetCommentReplies;

public record GetCommentRepliesResponse(
    IReadOnlyList<CommentReplyDto> Items,
    string? NextCursor,
    bool HasMore);

public record CommentReplyDto(
    long CommentId,
    long PostId,
    long ParentCommentId,
    int Depth,
    string AuthorId,
    string? Content,
    IReadOnlyList<MediaDto> Media,
    IReadOnlyList<MentionDto> Mentions,
    int RepliesCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
public record MediaDto(
string ObjectKey,
MediaType MediaType,
int DisplayOrder);

public record MentionDto(
    string UserId,
    int StartIndex,
    int Length);