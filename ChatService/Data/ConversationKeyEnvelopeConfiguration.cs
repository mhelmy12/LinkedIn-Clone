using ChatService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Data;

public class ConversationKeyEnvelopeConfiguration : IEntityTypeConfiguration<ConversationKeyEnvelope>
{
    public void Configure(EntityTypeBuilder<ConversationKeyEnvelope> builder)
    {
        builder.ToTable("ConversationKeyEnvelopes", table =>
        {
            table.HasCheckConstraint(
                "CK_ConversationKeyEnvelopes_Nonce_Size",
                "octet_length(\"Nonce\") = 12");
        });

        builder.HasKey(e => new { e.ConversationId, e.DeviceId });

        builder.Property(e => e.ConversationId)
            .ValueGeneratedNever();

        builder.Property(e => e.DeviceId)
            .ValueGeneratedNever();

        builder.Property(e => e.WrappedKey)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(e => e.Nonce)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(e => e.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // A device fetches its own envelopes when opening a conversation.
        builder.HasIndex(e => e.DeviceId)
            .HasDatabaseName("IX_ConversationKeyEnvelopes_Device");
    }
}
