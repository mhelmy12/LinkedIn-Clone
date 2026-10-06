using System;

namespace GraphService.Neo4j;

using global::Neo4j.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
public static class Neo4jExtensions
{
    public static IServiceCollection AddNeo4jDriver(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<Neo4jOptions>(
            configuration.GetSection(Neo4jOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Neo4jOptions>>().Value;

            // var uri = configuration.GetConnectionString("neo4j")
            //     ?? options.Uri;
            var uri = options.Uri;

            var username = options.Username;
            var password = options.Password;

            var driver = GraphDatabase.Driver(
                uri,
                AuthTokens.Basic(username, password),
                cfg =>
                {
                    cfg.WithMaxConnectionPoolSize(options.MaxConnectionPoolSize)
                       .WithConnectionTimeout(options.ConnectionTimeout)
                       .WithMaxTransactionRetryTime(options.MaxTransactionRetryTime);
                });

            return driver;
        });

        services.AddHostedService<Neo4jIndexInitializer>();

        return services;
    }
}