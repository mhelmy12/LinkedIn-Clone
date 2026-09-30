using System;
using EngagementService.Models;
using Microsoft.EntityFrameworkCore;

namespace EngagementService.Data;

public class EngagmentDbContext : DbContext
{

    public EngagmentDbContext(DbContextOptions<EngagmentDbContext> options) : base(options)
    {
    }

    public DbSet<Reaction> Reactions { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<CommentMedia> CommentMedia { get; set; }
    public DbSet<CommentMention> CommentMentions { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EngagmentDbContext).Assembly);

    }


}
