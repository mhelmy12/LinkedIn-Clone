using System;

namespace GraphService.Features.GetSuggestions;

public record GetSuggestionsResponse(
    IReadOnlyList<SuggestionDto> Suggestions);

public record SuggestionDto(
    string UserId,
    string DisplayName,
    string? ProfileImageKey,
    string? Headline,
    int MutualCount,
    IReadOnlyList<string> MutualFriendIds);