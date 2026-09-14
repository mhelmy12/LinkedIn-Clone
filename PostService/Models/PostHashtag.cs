using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostService.Models;

public class PostHashtag
{

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; } // Snowflake ID
    public long PostId { get; set; } // Snowflake ID
    public string Hashtag { get; set; } = string.Empty;

}
