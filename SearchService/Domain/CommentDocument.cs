using System;

namespace SearchService.Domain;

public class CommentDocument
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public string AuthorId { get; set; }
    public long? ParentCommentId { get; set; }
    public int Depth { get; set; }

    public string? AuthorDisplayName { get; set; }
    public string? AuthorProfileImageKey { get; set; }
    public string? AuthorHeadline { get; set; }

    public string? Content { get; set; }

    public List<string> MentionedUserIds { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime IndexedAt { get; set; }
}