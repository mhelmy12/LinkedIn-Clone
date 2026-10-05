using System;

namespace FeedService.Data;

using FeedService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


public class PostSnapshotConfiguration : IEntityTypeConfiguration<PostSnapshot>
{
    public void Configure(EntityTypeBuilder<PostSnapshot> builder)
    {
        builder.ToTable("PostSnapshots");

        builder.HasKey(p => p.PostId);

        builder.Property(p => p.PostId)
            .ValueGeneratedNever();

        builder.Property(p => p.AuthorId).IsRequired();

        // Author fields
        builder.Property(p => p.AuthorDisplayName).HasMaxLength(200);
        builder.Property(p => p.AuthorProfileImageKey).HasMaxLength(500);
        builder.Property(p => p.AuthorHeadline).HasMaxLength(500);

        // Content
        builder.Property(p => p.Content).HasMaxLength(3000);
        builder.Property(p => p.Visibility).IsRequired();
        builder.Property(p => p.IsPureRepost).IsRequired();
        builder.Property(p => p.RepostOfPostId);

        // RepostOf
        builder.Property(p => p.RepostOfContent).HasMaxLength(3000);
        builder.Property(p => p.RepostOfAuthorId);
        builder.Property(p => p.RepostOfAuthorDisplayName).HasMaxLength(200);

        builder.Property(p => p.Hashtags)
            .HasColumnType("text[]");

        builder.Property(p => p.MentionedUserIds)
            .HasColumnType("bigint[]");

        // Timestamps
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt);

        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(p => p.AuthorId)
            .HasDatabaseName("IX_PostSnapshots_AuthorId");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_PostSnapshots_CreatedAt");

        // initial fan-out: get recent posts by author, ordered by CreatedAt descending
        builder.HasIndex(p => new { p.AuthorId, p.CreatedAt })
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_PostSnapshots_Author_CreatedAt");

        // QueryFilter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}