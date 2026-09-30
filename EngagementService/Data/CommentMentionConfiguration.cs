using System;
using EngagementService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngagementService.Data;

public class CommentMentionConfiguration : IEntityTypeConfiguration<CommentMention>
{
    public void Configure(EntityTypeBuilder<CommentMention> builder)
    {
        builder.ToTable("CommentMentions");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.MentionedUserId).IsRequired();
        builder.Property(m => m.StartIndex).IsRequired();
        builder.Property(m => m.Length).IsRequired();

        builder.HasOne(m => m.Comment)
            .WithMany(c => c.Mentions)
            .HasForeignKey(m => m.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.CommentId);
        builder.HasIndex(m => m.MentionedUserId);

        builder.HasQueryFilter(m => !m.Comment.IsDeleted);
    }
}
