using ChatService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Data;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations", table =>
        {
            table.HasCheckConstraint(
                "CK_Conversations_Participants_Order",
                "\"Participant1Id\" < \"Participant2Id\"");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.Participant1Id)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.Participant2Id)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.LastMessageAt)
            .IsRequired();

        // Exactly one conversation per normalized participant pair (FR-003/SC-006).
        builder.HasIndex(c => new { c.Participant1Id, c.Participant2Id })
            .IsUnique()
            .HasDatabaseName("UX_Conversations_Participants");

        // Conversation lists are ordered by most recent activity (FR-013).
        builder.HasIndex(c => c.LastMessageAt)
            .IsDescending()
            .HasDatabaseName("IX_Conversations_LastMessageAt");
    }
}
