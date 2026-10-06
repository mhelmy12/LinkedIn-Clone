using System;

namespace FeedService.Features.GetFeed;

public record GetFeedResponse(
    IReadOnlyList<FeedItemDto> Items,
    string? NextCursor,
    bool HasMore);

public record FeedItemDto(
    long PostId,
    string AuthorId,
    string? AuthorDisplayName,
    string? AuthorProfileImageKey,
    string? AuthorHeadline,
    string? Content,
    int Visibility,
    bool IsPureRepost,
    long? RepostOfPostId,
    string? RepostOfContent,
    string? RepostOfAuthorId,
    string? RepostOfAuthorDisplayName,
    IReadOnlyList<string> Hashtags,
    IReadOnlyList<string> MentionedUserIds,
    FeedCountersDto Counters,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record FeedCountersDto(
    long Reactions,
    long Comments,
    long Reposts);