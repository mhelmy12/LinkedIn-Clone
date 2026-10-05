using System;
using FeedService.Models;
using Microsoft.EntityFrameworkCore;

namespace FeedService.Data;

public class FeedDbContext : DbContext
{
    public FeedDbContext(DbContextOptions<FeedDbContext> options) : base(options)
    {
    }
    public DbSet<PostSnapshot> PostSnapshots { get; set; }
    public DbSet<UserSummary> UserSummaries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FeedDbContext).Assembly);

        base.OnModelCreating(modelBuilder);

    }


}
