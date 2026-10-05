using System;

namespace FeedService.Helpers.RedisConfiguration;

public class FeedOptions
{
    public const string SectionName = "Feed";
    public int MaxFeedSize { get; set; } = 1000;
    public int InitialFanOutSize { get; set; } = 100;
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 50;
    public int FeedTtlDays { get; set; } = 30;
    public int ConnectionCacheTtlDays { get; set; } = 7;
    public int SnapshotCacheTtlHours { get; set; } = 1;
}