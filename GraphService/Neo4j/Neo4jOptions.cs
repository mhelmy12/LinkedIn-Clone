using System;

namespace GraphService.Neo4j;

public class Neo4jOptions
{
    public const string SectionName = "Neo4j";

    public string Uri { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    public int MaxConnectionPoolSize { get; set; } = 100;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan MaxTransactionRetryTime { get; set; } = TimeSpan.FromSeconds(30);
}