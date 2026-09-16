using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PostService.Models;

namespace PostService.Data;

public class PostMentionConfiguration : IEntityTypeConfiguration<PostMention>
{
    public void Configure(EntityTypeBuilder<PostMention> builder)
    {
        builder.ToTable("PostMentions");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MentionedEntityId)
            .IsRequired();

        builder.Property(m => m.StartIndex)
            .IsRequired();

        builder.Property(m => m.Length)
            .IsRequired();

        builder.HasOne(m => m.Post)
            .WithMany(p => p.Mentions)
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.PostId);
        builder.HasIndex(m => m.MentionedEntityId);
    }
}