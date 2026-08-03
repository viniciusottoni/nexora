using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("device");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(d => d.TenantId).HasColumnName("tenant_id");
        builder.Property(d => d.StoreId).HasColumnName("store_id");
        builder.Property(d => d.Label).HasColumnName("label").IsRequired();
        builder.Property(d => d.Type).HasColumnName("type").IsRequired();
        builder.Property(d => d.Fingerprint).HasColumnName("fingerprint").IsRequired();
        builder.Property(d => d.SecretHash).HasColumnName("secret_hash");
        builder.Property(d => d.StationId).HasColumnName("station_id");
        builder.Property(d => d.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(d => d.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamptz");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(d => new { d.TenantId, d.Fingerprint }).IsUnique().HasDatabaseName("uq_device_fingerprint");
        builder.HasIndex(d => d.TenantId).HasDatabaseName("idx_device_tenant");

        builder.HasOne(d => d.Tenant).WithMany(t => t.Devices).HasForeignKey(d => d.TenantId);
        builder.HasOne(d => d.Store).WithMany(s => s.Devices).HasForeignKey(d => d.StoreId);
    }
}
