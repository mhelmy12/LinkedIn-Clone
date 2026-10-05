using System;
using FeedService.Abstractions;
using FeedService.Redis;

namespace FeedService.Helpers.RedisConfiguration;

public static class RedisExtensions
{
    public static IServiceCollection AddRedisCaches(
        this IServiceCollection services)
    {
        services.AddSingleton<IFeedCache, RedisFeedCache>();
        services.AddSingleton<ICountersCache, RedisCountersCache>();
        // services.AddSingleton<IConnectionsCache, RedisConnectionsCache>();

        return services;
    }
}