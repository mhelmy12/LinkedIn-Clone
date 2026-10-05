using System;
using Microsoft.EntityFrameworkCore;

namespace FeedService.Data;

public class FeedDbContext : DbContext
{
    public FeedDbContext(DbContextOptions<FeedDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
           modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FeedDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

    }


}
