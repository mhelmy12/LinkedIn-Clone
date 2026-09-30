using System;
using EngagementService.Enums;

namespace EngagementService.Models;

public class Reaction
{
    public long Id { get; set; }
    public string UserId { get; set; }
    public TargetType TargetType { get; set; }
    public string TargetId { get; set; }
    public ReactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

}
