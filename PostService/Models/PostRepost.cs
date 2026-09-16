using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostService.Models;

public class PostRepost
{

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; } // Snowflake ID
    public long OriginalPostId { get; set; }

    public string UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

}
