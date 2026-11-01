using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatService.Data;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> so migrations can be created
/// without a running Aspire host. The connection string is only used for
/// provider metadata; no database connection is opened while scaffolding.
/// </summary>
public class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__ChatDbConnection")
            ?? "Host=localhost;Port=5432;Database=chatDb;Username=postgres";

        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ChatDbContext(options);
    }
}
