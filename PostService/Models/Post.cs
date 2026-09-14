using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostService.Models;

public class Post
{


    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Key]

    public long Id { get; set; } // Snowflake ID

    public long AuthorId { get; set; }

    public AuthorType AuthorType { get; set; } = AuthorType.User;

    public string Content { get; set; } = string.Empty;

    public PostType Type { get; set; }

    public long? OriginalPostId { get; set; }

    public VisibilityType Visibility { get; set; } = VisibilityType.Public;

    public StatusType Status { get; set; } = StatusType.Draft;

    public long ReactionCount { get; set; } = 0;

    public long CommentCount { get; set; } = 0;

    public long RepostCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; } = null;

}

public enum AuthorType
{
    User,
    Company
}

public enum VisibilityType
{
    Public,
    Connections
}

public enum StatusType
{
    Draft,
    Published
}

public enum PostType
{
    Original,
    Quote,
    Article,
    Poll

}
