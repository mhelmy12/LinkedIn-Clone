using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PostService.Models;

namespace PostService.Data;

public class PostMediaConfiguration : IEntityTypeConfiguration<PostMedia>
{
    public void Configure(EntityTypeBuilder<PostMedia> builder)
    {
        builder.ToTable("PostMedia");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MediaKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.MediaType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.DisplayOrder)
            .IsRequired();

        builder.HasOne(m => m.Post)
            .WithMany(p => p.Media)
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.PostId);
    }
}