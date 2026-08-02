using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class PairingCodeConfiguration : IEntityTypeConfiguration<PairingCode>
{
    public void Configure(EntityTypeBuilder<PairingCode> builder)
    {
        builder.ToTable("pairing_code");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.StoreId).HasColumnName("store_id");
        builder.Property(p => p.CodeHash).HasColumnName("code_hash").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
        builder.Property(p => p.ConsumedAt).HasColumnName("consumed_at").HasColumnType("timestamptz");
        builder.Property(p => p.Attempts).HasColumnName("attempts").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasIndex(p => new { p.TenantId, p.ExpiresAt }).HasDatabaseName("idx_pairing_code_tenant_expires");
    }
}
