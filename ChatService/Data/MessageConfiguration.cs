using ChatService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Data;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages", table =>
        {
            // The server only ever stores ciphertext; enforce the transport ceiling
            // and the AES-GCM nonce size at the database boundary (FR-021).
            table.HasCheckConstraint(
                "CK_Messages_Ciphertext_Size",
                "octet_length(\"Ciphertext\") <= 16384");

            table.HasCheckConstraint(
                "CK_Messages_Nonce_Size",
                "octet_length(\"Nonce\") = 12");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.ConversationId)
            .IsRequired();

        builder.Property(m => m.SenderId)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(m => m.ClientMessageId)
            .IsRequired();

        builder.Property(m => m.Ciphertext)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(m => m.Nonce)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(m => m.Algorithm)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(m => m.SentAt)
            .IsRequired();

        builder.Property(m => m.ReadAtUtc);

        builder.HasOne(m => m.Conversation)
            .WithMany()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Send retries and double taps must not create duplicate messages (FR-019/FR-023).
        builder.HasIndex(m => new { m.ConversationId, m.SenderId, m.ClientMessageId })
            .IsUnique()
            .HasDatabaseName("UX_Messages_Sender_ClientMessageId");

        // Keyset history paging, newest first (FR-022, research R9).
        builder.HasIndex(m => new { m.ConversationId, m.Id })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Messages_Conversation_Id");

        // Unread counters (FR-012) only need messages that are still unread.
        builder.HasIndex(m => m.ConversationId)
            .HasFilter("\"ReadAtUtc\" IS NULL")
            .HasDatabaseName("IX_Messages_Unread");
    }
}
