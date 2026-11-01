using ChatService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Data;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.Type)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.AggregateType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.AggregateId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.Payload)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(o => o.Timestamp)
            .IsRequired();

        // Debezium reads rows in insertion order; keep the CDC scan cheap.
        builder.HasIndex(o => o.Timestamp)
            .HasDatabaseName("IX_OutboxMessages_Timestamp");
    }
}
