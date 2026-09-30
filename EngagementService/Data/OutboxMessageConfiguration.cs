using System;
using EngagementService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngagementService.Data;

public class OutboxMessageConfiguration
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

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

        builder.HasIndex(o => o.Timestamp);

    }
}
