using System;

namespace GraphService.Models;

public class SuggestionResult
{
    public string UserId { get; set; }
    public string DisplayName { get; set; } = default!;
    public string? ProfileImageKey { get; set; }
    public string? Headline { get; set; }
    public int MutualCount { get; set; }
    public List<string> MutualFriendIds { get; set; } = new();
}