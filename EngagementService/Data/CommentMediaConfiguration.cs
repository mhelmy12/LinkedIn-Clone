using System;
using EngagementService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngagementService.Data;

public class CommentMediaConfiguration : IEntityTypeConfiguration<CommentMedia>
{
    public void Configure(EntityTypeBuilder<CommentMedia> builder)
    {
        builder.ToTable("CommentMedia");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.ObjectKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.MediaType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.DisplayOrder).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();

        builder.HasOne(m => m.Comment)
            .WithMany(c => c.Media)
            .HasForeignKey(m => m.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.CommentId);

        builder.HasQueryFilter(m => !m.Comment.IsDeleted);
    }
}
