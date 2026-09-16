using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostService.Models;

public class PostMention
{

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; } // Snowflake ID
    public long PostId { get; set; } // Snowflake ID
    public string MentionedEntityId { get; set; } // Snowflake ID
    public int StartIndex { get; set; }
    public int Length { get; set; }

    public Post Post { get; set; } = default!;

}
