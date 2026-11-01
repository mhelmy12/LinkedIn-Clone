using ChatService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Data;

public class UserDeviceKeyConfiguration : IEntityTypeConfiguration<UserDeviceKey>
{
    public void Configure(EntityTypeBuilder<UserDeviceKey> builder)
    {
        builder.ToTable("UserDeviceKeys");

        builder.HasKey(k => k.DeviceId);

        builder.Property(k => k.DeviceId)
            .ValueGeneratedNever();

        builder.Property(k => k.UserId)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(k => k.PublicKey)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(k => k.Algorithm)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(k => k.RegisteredAt)
            .IsRequired();

        builder.Property(k => k.RevokedAt);

        // Key exchange only ever considers active devices (FR-017).
        builder.HasIndex(k => k.UserId)
            .HasFilter("\"RevokedAt\" IS NULL")
            .HasDatabaseName("IX_UserDeviceKeys_User_Active");
    }
}
