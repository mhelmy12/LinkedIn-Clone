using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PostService.Models;

namespace PostService.Data;

public class PostRepostConfiguration : IEntityTypeConfiguration<PostRepost>
{
    public void Configure(EntityTypeBuilder<PostRepost> builder)
    {
        builder.ToTable("PostReposts");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OriginalPostId).IsRequired();
        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();



        builder.HasOne<Post>().WithMany().HasForeignKey(r => r.OriginalPostId);

        builder.HasIndex(r => new { r.OriginalPostId, r.UserId })
            .IsUnique();

        builder.HasIndex(r => r.OriginalPostId);
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.CreatedAt);
    }
}