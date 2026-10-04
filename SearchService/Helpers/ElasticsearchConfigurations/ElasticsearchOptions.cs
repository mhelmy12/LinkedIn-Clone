using System;

namespace SearchService.Helpers.ElasticsearchConfigurations;

public class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string Url { get; set; } = "http://localhost:9200";

    public string PostsIndexAlias { get; set; } = "posts";
    public string PostsIndexName { get; set; } = "posts-v1";

    public string UsersIndexAlias { get; set; } = "users";
    public string UsersIndexName { get; set; } = "users-v1";

    public string EngagementsIndexAlias { get; set; } = "engagements";
    public string EngagementsIndexName { get; set; } = "engagements-v1";

    public int Shards { get; set; } = 1;
    public int Replicas { get; set; } = 0;
}