using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class EdgeInstallationConfiguration : IEntityTypeConfiguration<EdgeInstallation>
{
    public void Configure(EntityTypeBuilder<EdgeInstallation> builder)
    {
        builder.ToTable("edge_installation");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.StoreId).HasColumnName("store_id");
        builder.Property(e => e.Label).HasColumnName("label").IsRequired();
        builder.Property(e => e.PublicKey).HasColumnName("public_key"); // Ed25519 — ADR-031
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(20);
        builder.Property(e => e.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamptz");
        builder.Property(e => e.LastSyncedSeq).HasColumnName("last_synced_seq").HasDefaultValue(0L);
        builder.Property(e => e.ClockOffsetMs).HasColumnName("clock_offset_ms"); // ADR-034
        builder.Property(e => e.Health).HasColumnName("health").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(e => e.InstallTokenHash).HasColumnName("install_token_hash");
        builder.Property(e => e.TokenExpiresAt).HasColumnName("token_expires_at").HasColumnType("timestamptz");
        builder.Property(e => e.TokenConsumedAt).HasColumnName("token_consumed_at").HasColumnType("timestamptz");
        builder.Property(e => e.InstalledAt).HasColumnName("installed_at").HasColumnType("timestamptz");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(e => e.TenantId).HasDatabaseName("idx_edge_tenant");
        builder.HasIndex(e => e.StoreId).IsUnique().HasDatabaseName("uq_edge_store");
        builder.HasIndex(e => e.LastSeenAt).HasDatabaseName("idx_edge_last_seen").HasFilter("last_seen_at IS NOT NULL");

        builder.HasOne(e => e.Tenant).WithMany(t => t.EdgeInstallations).HasForeignKey(e => e.TenantId);
        builder.HasOne(e => e.Store).WithMany(s => s.EdgeInstallations).HasForeignKey(e => e.StoreId);
    }
}
