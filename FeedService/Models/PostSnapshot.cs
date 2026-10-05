using System;

namespace FeedService.Models;

public class PostSnapshot
{
    public long PostId { get; set; }
    public string AuthorId { get; set; }
    public string? AuthorDisplayName { get; set; }
    public string? AuthorProfileImageKey { get; set; }
    public string? AuthorHeadline { get; set; }
    public string? Content { get; set; }
    public int Visibility { get; set; }
    public bool IsPureRepost { get; set; }
    public long? RepostOfPostId { get; set; }
    public string? RepostOfContent { get; set; }
    public string? RepostOfAuthorId { get; set; }
    public string? RepostOfAuthorDisplayName { get; set; }
    public List<string> Hashtags { get; set; } = new();
    public List<string> MentionedUserIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
