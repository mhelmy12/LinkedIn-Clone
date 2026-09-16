using System;
using Microsoft.EntityFrameworkCore;
using PostService.Models;

namespace PostService.Data;

public class PostDbContext : DbContext
{
    public PostDbContext(DbContextOptions<PostDbContext> options) : base(options)
    {
    }

    public DbSet<Post> Posts { get; set; }
    public DbSet<PostMention> PostMentions { get; set; }
    public DbSet<PostMedia> PostMedia { get; set; }
    public DbSet<PostHashtag> PostHashtags { get; set; }

    public DbSet<PostRepost> PostReposts { get; set; }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }




    override protected void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PostDbContext).Assembly);




    }


}
