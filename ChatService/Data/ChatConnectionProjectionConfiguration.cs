using ChatService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Data;

public class ChatConnectionProjectionConfiguration : IEntityTypeConfiguration<ChatConnectionProjection>
{
    public void Configure(EntityTypeBuilder<ChatConnectionProjection> builder)
    {
        builder.ToTable("ChatConnectionProjections");

        builder.HasKey(p => new { p.UserId, p.OtherUserId });

        builder.Property(p => p.UserId)
            .ValueGeneratedNever()
            .HasMaxLength(255);

        builder.Property(p => p.OtherUserId)
            .ValueGeneratedNever()
            .HasMaxLength(255);

        builder.Property(p => p.IsActive)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        // Active-connection lookups on the send path (FR-001/FR-002).
        builder.HasIndex(p => new { p.UserId, p.IsActive })
            .HasDatabaseName("IX_ChatConnectionProjections_User_Active");
    }
}
