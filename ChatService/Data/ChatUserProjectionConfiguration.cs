using ChatService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Data;

public class ChatUserProjectionConfiguration : IEntityTypeConfiguration<ChatUserProjection>
{
    public void Configure(EntityTypeBuilder<ChatUserProjection> builder)
    {
        builder.ToTable("ChatUserProjections");

        builder.HasKey(p => p.UserId);

        builder.Property(p => p.UserId)
            .ValueGeneratedNever()
            .HasMaxLength(255);

        builder.Property(p => p.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Email)
            .HasMaxLength(320);

        builder.Property(p => p.UpdatedAt)
            .IsRequired();
    }
}
