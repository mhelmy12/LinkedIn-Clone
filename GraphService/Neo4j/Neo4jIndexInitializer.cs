using System;

namespace GraphService.Neo4j;

using global::Neo4j.Driver;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


public class Neo4jIndexInitializer : IHostedService
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jIndexInitializer> _logger;

    public Neo4jIndexInitializer(
        IDriver driver,
        ILogger<Neo4jIndexInitializer> logger)
    {
        _driver = driver;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var session = _driver.AsyncSession();

            await session.RunAsync(@"
                CREATE INDEX user_id_idx IF NOT EXISTS
                FOR (u:User) ON (u.userId)");

            _logger.LogInformation(
                "Neo4j initialized: user_id_idx ensured");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to initialize Neo4j indexes. Service may not work correctly.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}