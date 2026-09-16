using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PostService.Models;

namespace PostService.Data;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.AggregateId)
            .IsRequired();

        builder.Property(o => o.Payload)
            .IsRequired()
            .HasColumnType("VARCHAR(MAX)");

    }
}
