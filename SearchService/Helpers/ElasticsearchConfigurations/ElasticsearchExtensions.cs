using System;

namespace SearchService.Helpers.ElasticsearchConfigurations;

using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
public static class ElasticsearchExtensions
{
    public static IServiceCollection AddElasticsearchClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ElasticsearchOptions>(
            configuration.GetSection(ElasticsearchOptions.SectionName));

        services.AddSingleton<ElasticsearchClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
            // var url = configuration.GetConnectionString("elasticsearch")
            //     ?? options.Url;

            var url = options.Url;
            var settings = new ElasticsearchClientSettings(new Uri(url))
                .DefaultIndex(options.PostsIndexAlias)
                .EnableDebugMode();

            return new ElasticsearchClient(settings);
        });

        return services;
    }
}