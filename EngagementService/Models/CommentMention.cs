using System;

namespace EngagementService.Models;

public class CommentMention
{
    public long Id { get; set; }
    public long CommentId { get; set; }
    public string MentionedUserId { get; set; }
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public Comment Comment { get; set; } = default!;

}
