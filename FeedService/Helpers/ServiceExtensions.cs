using System;
using FeedService.Abstractions;
using FeedService.Data;
using FeedService.Redis;

namespace FeedService.Helpers;

public static class ServiceExtensions
{
    public static IServiceCollection AddFeddInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<IFeedCache, RedisFeedCache>();
        services.AddSingleton<ICountersCache, RedisCountersCache>();
        services.AddSingleton<IConnectionsCache, RedisConnectionsCache>();
        services.AddScoped<ISnapshotStore, PostSnapshotStore>();
        services.AddScoped<IUserSummaryStore, UserSummaryStore>();

        return services;
    }
}