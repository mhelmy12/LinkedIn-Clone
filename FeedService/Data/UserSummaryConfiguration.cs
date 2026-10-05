using System;

namespace FeedService.Data;

using FeedService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


public class UserSummaryConfiguration : IEntityTypeConfiguration<UserSummary>
{
    public void Configure(EntityTypeBuilder<UserSummary> builder)
    {
        builder.ToTable("UserSummaries");

        builder.HasKey(u => u.UserId);
        builder.Property(u => u.UserId).ValueGeneratedNever();

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.ProfileImageKey)
            .HasMaxLength(500);

        builder.Property(u => u.Headline)
            .HasMaxLength(500);

        builder.Property(u => u.UpdatedAt).IsRequired();

        builder.HasIndex(u => u.UpdatedAt)
            .HasDatabaseName("IX_UserSummaries_UpdatedAt");
    }
}