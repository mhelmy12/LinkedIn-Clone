using System;
using PostService.Models;

namespace PostService.Features.GetPostById;

public record GetPostByIdQueryResponse(
    string Id,
    string CanonicalPostId,
    string AuthorId,
    string? Content,
    int Visibility,
    bool IsPureRepost,

    string? RepostOfPostId,
    RepostOfDto? RepostOf,
    bool IsRepostOfUnavailable,

    IReadOnlyList<MediaDto> Media,
    IReadOnlyList<MentionedUserDto> MentionedUserIds,
    IReadOnlyList<string> Hashtags,

    DateTime CreatedAt,
    DateTime? UpdatedAt);
public record RepostOfDto(
    string Id,
    string AuthorId,
    string? Content,
    IReadOnlyList<MediaDto> Media,
    IReadOnlyList<MentionedUserDto> MentionedUserIds,
    IReadOnlyList<string> Hashtags,
    DateTime CreatedAt);

public record MediaDto(
    string ObjectKey,
    int MediaType,
    int DisplayOrder);


public record MentionedUserDto(
    string UserId,
    int StartIndex,
    int Length
    );