using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

/// <summary>US-156 · Recuperação do provisionamento e token de instalação — mapeamento de <see cref="InstallationCredential"/>.</summary>
internal sealed class InstallationCredentialConfiguration : IEntityTypeConfiguration<InstallationCredential>
{
    public void Configure(EntityTypeBuilder<InstallationCredential> builder)
    {
        builder.ToTable("installation_credential");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.InstallationId).HasColumnName("installation_id");
        builder.Property(c => c.TokenHash).HasColumnName("token_hash").IsRequired();
        builder.Property(c => c.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
        builder.Property(c => c.ConsumedAt).HasColumnName("consumed_at").HasColumnType("timestamptz");
        builder.Property(c => c.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");
        builder.Property(c => c.Reason).HasColumnName("reason");
        builder.Property(c => c.ActorId).HasColumnName("actor_id");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(c => new { c.TenantId, c.InstallationId }).HasDatabaseName("idx_installation_credential_installation");

        // token_hash já é opaco (SHA-256) e, por construção, único (o texto bruto é aleatório de
        // alta entropia) — a constraint dobra como defesa em profundidade contra colisão E contra
        // um bug de reemissão que reaproveitasse acidentalmente o mesmo segredo.
        builder.HasIndex(c => c.TokenHash).IsUnique().HasDatabaseName("uq_installation_credential_token_hash");

        // Suporta a checagem "só uma credencial pendente por instalação" (US-156, cenário de
        // concorrência — duas reemissões simultâneas não podem deixar duas linhas pendentes).
        builder.HasIndex(c => new { c.InstallationId, c.RevokedAt, c.ConsumedAt })
            .HasDatabaseName("idx_installation_credential_pending")
            .HasFilter("revoked_at IS NULL AND consumed_at IS NULL");

        // Sem navegação de volta em EdgeInstallation (mesmo padrão de InstallationIncidentConfiguration)
        // — InstallationCredential é uma entidade irmã, referenciando por FK, não uma coleção filha
        // do agregado (ver docstring de InstallationCredential sobre a decisão de convivência).
        builder.HasOne<EdgeInstallation>().WithMany().HasForeignKey(c => c.InstallationId);
    }
}
