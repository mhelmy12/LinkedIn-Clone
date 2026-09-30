using System;
using EngagementService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngagementService.Data;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.PostId).IsRequired();
        builder.Property(c => c.AuthorId).IsRequired();
        builder.Property(c => c.ParentCommentId).IsRequired(false);

        builder.Property(c => c.Content)
            .HasMaxLength(3000);

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt);

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne<Comment>()
            .WithMany()
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ⭐ Relations
        builder.HasMany(c => c.Media)
            .WithOne(m => m.Comment)
            .HasForeignKey(m => m.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Mentions)
            .WithOne(m => m.Comment)
            .HasForeignKey(m => m.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(c => !c.IsDeleted);


        builder.HasIndex(c => new { c.PostId, c.ParentCommentId, c.CreatedAt })
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Comments_Post_TopLevel");

        builder.HasIndex(c => new { c.ParentCommentId, c.CreatedAt })
            .HasFilter("\"IsDeleted\" = false AND \"ParentCommentId\" IS NOT NULL")
            .HasDatabaseName("IX_Comments_Replies");

        builder.HasIndex(c => new { c.AuthorId, c.CreatedAt })
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Comments_Author");
    }
}