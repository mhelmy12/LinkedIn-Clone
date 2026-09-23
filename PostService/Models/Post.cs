using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PostService.Models;

public class Post
{


    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Key]

    public long Id { get; set; } // Snowflake ID

    public string AuthorId { get; set; }


    public string? Content { get; set; } = string.Empty;

    public PostType Type { get; set; }

    public long? RepostOfPostId { get; set; }

    public VisibilityType Visibility { get; set; } = VisibilityType.Public;

    public StatusType Status { get; set; } = StatusType.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; } = null;

    public bool IsDeleted { get; set; } = false;
    public byte[]? RowVersion { get; set; }

    public ICollection<PostMedia> Media { get; set; } = new List<PostMedia>();
    public ICollection<PostMention> Mentions { get; set; } = new List<PostMention>();
    public ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();

}


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VisibilityType
{
    Public,
    Private,
    Connections
}



[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusType
{
    Draft,
    Published
}



[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PostType
{
    Original,
    PureRepost,
    Quote,
    Article,
    Poll

}
