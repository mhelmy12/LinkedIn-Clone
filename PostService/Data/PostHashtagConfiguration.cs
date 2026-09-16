using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PostService.Models;

namespace PostService.Data;

public class PostHashtagConfiguration : IEntityTypeConfiguration<PostHashtag>
{
    public void Configure(EntityTypeBuilder<PostHashtag> builder)
    {
        builder.ToTable("PostHashtags");
        builder.HasKey(ph => new { ph.PostId, ph.NormalizedName });

        builder.Property(ph => ph.NormalizedName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne<Post>()
            .WithMany(p => p.PostHashtags)
            .HasForeignKey(ph => ph.PostId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}