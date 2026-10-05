using System;
using FeedService.Abstractions;
using FeedService.Data;
using FeedService.Redis;
using FeedService.Services;

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
        services.AddScoped<IFeedFanOutService, FeedFanOutService>();

        return services;
    }
}