using System;

namespace PostService.Features.GetPostReposts;

public record GetPostRepostsQueryResponse(
    IReadOnlyList<RepostSummaryDto> Items,
    string? NextCursor,
    bool HasMore);

public record RepostSummaryDto(
    string Id,
    string CanonicalPostId,
    string AuthorId,
    AuthorDto? Author,
    string? Content,                    // null = Pure Repost
    bool IsPureRepost,
    IReadOnlyList<MediaDto> Media,
    IReadOnlyList<MentionDto> Mentions,
    IReadOnlyList<string> Hashtags,
    PostCountersDto Counters,
    DateTime CreatedAt,
    DateTime? UpdatedAt);


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