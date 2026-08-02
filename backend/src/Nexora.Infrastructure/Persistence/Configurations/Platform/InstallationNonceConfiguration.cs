using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class InstallationNonceConfiguration : IEntityTypeConfiguration<InstallationNonce>
{
    public void Configure(EntityTypeBuilder<InstallationNonce> builder)
    {
        builder.ToTable("installation_nonce");

        // chave composta — anti-replay do protocolo de autenticação da instalação edge
        builder.HasKey(n => new { n.InstallationId, n.Nonce });

        builder.Property(n => n.InstallationId).HasColumnName("installation_id");
        builder.Property(n => n.TenantId).HasColumnName("tenant_id");
        builder.Property(n => n.Nonce).HasColumnName("nonce").HasMaxLength(128).IsRequired();
        builder.Property(n => n.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasIndex(n => new { n.TenantId, n.ExpiresAt }).HasDatabaseName("idx_installation_nonce_tenant_expires");
    }
}
