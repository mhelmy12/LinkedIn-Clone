using System;

namespace PostService.Features.GetPostByAuthor;

public record GetPostsByAuthorQueryResponse(
    IReadOnlyList<PostSummaryDto> Items,
    string? NextCursor,
    bool HasMore);


public record PostSummaryDto(
    string Id,
    string CanonicalPostId,
    string AuthorId,
    AuthorDto? Author,
    string? Content,
    int Visibility,
    bool IsPureRepost,

    string? RepostOfPostId,
    RepostOfSummaryDto? RepostOf,
    bool IsRepostOfUnavailable,

    IReadOnlyList<MediaDto> Media,
    IReadOnlyList<MentionDto> Mentions,
    IReadOnlyList<string> Hashtags,

    PostCountersDto Counters,

    DateTime CreatedAt,
    DateTime? UpdatedAt);


public record RepostOfSummaryDto(
string Id,
string AuthorId,
AuthorDto? Author,
string? Content,
IReadOnlyList<MediaDto> Media,
IReadOnlyList<MentionDto> Mentions,
IReadOnlyList<string> Hashtags,
DateTime CreatedAt);

public record AuthorDto(
string UserId,
string DisplayName,
string? ProfileImageKey,
string? Headline);

public record MediaDto(
string MediaKey,
int MediaType,
int DisplayOrder);

public record MentionDto(
string UserId,
string DisplayName,
string? ProfileImageKey,
int StartIndex,
int Length);

public record PostCountersDto(
int Reactions,
int Comments,
int Reposts);