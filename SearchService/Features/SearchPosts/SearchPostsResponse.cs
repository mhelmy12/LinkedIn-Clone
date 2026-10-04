using System;

namespace SearchService.Features.SearchPosts;

public record SearchPostsResponse(
    IReadOnlyList<PostSearchResultDto> Items,
    string? NextCursor,
    long TotalHits);

public record PostSearchResultDto(
    long Id,
    string AuthorId,
    string? Content,
    IReadOnlyList<string> Hashtags,
    IReadOnlyList<string> MentionedUserIds,
    DateTime CreatedAt,
    DateTime? UpdatedAt);