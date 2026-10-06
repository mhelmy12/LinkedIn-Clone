using System;

namespace GraphService.Models;


public class MutualConnectionResult
{
    public string UserId { get; set; }
    public string DisplayName { get; set; } = default!;
    public string? ProfileImageKey { get; set; }
    public string? Headline { get; set; }
}