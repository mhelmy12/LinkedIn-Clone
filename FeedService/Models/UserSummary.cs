using System;

namespace FeedService.Models;

public class UserSummary
{
    public string UserId { get; set; }
    public string DisplayName { get; set; } = default!;
    public string? ProfileImageKey { get; set; }
    public string? Headline { get; set; }
    public DateTime UpdatedAt { get; set; }
}