using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PostService.Models;

namespace PostService.Data;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.AuthorId)
            .IsRequired();

        builder.Property(p => p.Content)
            .HasMaxLength(3000);

        builder.Property(p => p.Visibility)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.RepostOfPostId)
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(p => p.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Relationships
        builder.HasMany(p => p.Media)
            .WithOne(m => m.Post)
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Mentions)
            .WithOne(m => m.Post)
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.HasOne<Post>()
            .WithMany()
            .HasForeignKey(p => p.RepostOfPostId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => p.AuthorId);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.RepostOfPostId);

        // Soft Delete Query Filter
        builder.HasQueryFilter(p => p.IsDeleted == false);
    }
}