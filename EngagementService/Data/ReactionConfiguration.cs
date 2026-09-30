using System;
using EngagementService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngagementService.Data;

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("Reactions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.TargetType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.TargetId)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt);

        builder.Property(r => r.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(r => new { r.UserId, r.TargetType, r.TargetId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Reactions_Active_User_Target");

        // forGetReactionsByTarget
        builder.HasIndex(r => new { r.TargetType, r.TargetId, r.CreatedAt })
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Reactions_Target_Active");

        // for GetUserReactions
        builder.HasIndex(r => new { r.UserId, r.CreatedAt })
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Reactions_User_Active");


        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}