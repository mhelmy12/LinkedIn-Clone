using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostService.Models;

public class PostHashtag
{

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long PostId { get; set; } // Snowflake ID
    public string NormalizedName { get; set; } = default!;
}
