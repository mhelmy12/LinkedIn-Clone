using System;

namespace EngagementService.Models;

public class CommentMedia
{
    public long Id { get; set; }
    public long CommentId { get; set; }

    public string ObjectKey { get; set; } = default!;

    public MediaType MediaType { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public Comment Comment { get; set; } = default!;

}

public enum MediaType
{
    Image,
    Video,
    Audio,
    Document
}
