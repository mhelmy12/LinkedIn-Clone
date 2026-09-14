using System;

namespace PostService.Models;

public class PostMedia
{

    public long Id { get; set; } // Snowflake ID

    public long PostId { get; set; } // Snowflake ID

    public string MediaKey { get; set; } = string.Empty;

    public MediaType MediaType { get; set; }

    public int DisplayOrder { get; set; } = 0;

}


public enum MediaType
{
    Image,
    Video,
    Document
}
