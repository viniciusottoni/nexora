using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class TenantSecretConfiguration : IEntityTypeConfiguration<TenantSecret>
{
    public void Configure(EntityTypeBuilder<TenantSecret> builder)
    {
        builder.ToTable("tenant_secret");
        // Nunca retorna pela API, nem para OWNER. Nunca trafega para o edge. (ADR-031)

        builder.HasKey(s => new { s.TenantId, s.Key });

        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.Key).HasColumnName("key").IsRequired();
        builder.Property(s => s.Ciphertext).HasColumnName("ciphertext").HasColumnType("bytea").IsRequired(); // AES-256-GCM
        builder.Property(s => s.KeyVersion).HasColumnName("key_version").IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
    }
}
