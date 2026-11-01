using ChatService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Data;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.HasKey(i => new { i.ConsumerGroup, i.EventId });

        builder.Property(i => i.ConsumerGroup)
            .ValueGeneratedNever()
            .HasMaxLength(100);

        builder.Property(i => i.EventId)
            .ValueGeneratedNever();

        builder.Property(i => i.ProcessedAt)
            .IsRequired();
    }
}
