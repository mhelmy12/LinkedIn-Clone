using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostService.Models;

public class PostRepost
{

    public long OriginalPostId { get; set; }

    public long UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

}
