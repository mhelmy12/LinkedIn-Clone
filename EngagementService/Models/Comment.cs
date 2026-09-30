using System;

namespace EngagementService.Models;
public class Comment
{
    public long Id { get; set; }
    public string PostId { get; set; }
    public string AuthorId { get; set; }
    public long? ParentCommentId { get; set; }

    public string? Content { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation
    public ICollection<CommentMedia> Media { get; set; } = new List<CommentMedia>();
    public ICollection<CommentMention> Mentions { get; set; } = new List<CommentMention>();
}