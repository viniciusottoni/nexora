using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_key"); // ADR-020

        builder.HasKey(k => k.Key);
        // chave gerada pelo cliente quando a intenção nasce — não é UUIDv7 da origem (ADR-020)
        builder.Property(k => k.Key).HasColumnName("key").ValueGeneratedNever().IsRequired();

        // Nullable de propósito — ver comentário em Nexora.Domain.Platform.IdempotencyKey
        // (rotas de plataforma/pareamento/instalação escrevem sem tenant ainda resolvido).
        builder.Property(k => k.TenantId).HasColumnName("tenant_id").IsRequired(false);
        builder.Property(k => k.Endpoint).HasColumnName("endpoint").IsRequired();
        builder.Property(k => k.RequestHash).HasColumnName("request_hash").IsRequired();
        builder.Property(k => k.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
        builder.Property(k => k.ResponseStatus).HasColumnName("response_status");
        builder.Property(k => k.ResponseBody).HasColumnName("response_body").HasColumnType("jsonb");
        builder.Property(k => k.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(k => k.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");

        builder.HasIndex(k => new { k.TenantId, k.ExpiresAt }).HasDatabaseName("idx_idempotency_key_tenant_expires");
    }
}
