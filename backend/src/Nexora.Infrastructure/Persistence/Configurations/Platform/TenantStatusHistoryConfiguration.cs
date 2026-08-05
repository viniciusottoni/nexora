using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

/// <summary>US-153 · Ciclo de vida do estabelecimento — histórico imutável de transições de status (doc §8).</summary>
internal sealed class TenantStatusHistoryConfiguration : IEntityTypeConfiguration<TenantStatusHistory>
{
    public void Configure(EntityTypeBuilder<TenantStatusHistory> builder)
    {
        builder.ToTable("tenant_status_history");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(h => h.TenantId).HasColumnName("tenant_id");
        builder.Property(h => h.PreviousStatus).HasColumnName("previous_status");
        builder.Property(h => h.NewStatus).HasColumnName("new_status");
        builder.Property(h => h.Reason).HasColumnName("reason").IsRequired();
        builder.Property(h => h.ActorId).HasColumnName("actor_id");
        builder.Property(h => h.Origin).HasColumnName("origin").HasMaxLength(32).IsRequired();
        builder.Property(h => h.DomainEventId).HasColumnName("domain_event_id");
        builder.Property(h => h.EffectiveAt).HasColumnName("effective_at").HasColumnType("timestamptz");
        builder.Property(h => h.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        // tenant_status_history não tem tenant_id em SEGUNDO plano (RLS, ADR-004) porque é filha de
        // tenant (a única tabela sem RLS): mesma exceção documentada em Tenant.OwnerEmail/TemplateCode
        // — mas diferente daquelas, esta tabela É de negócio comum (não denormalização de leitura), e
        // vive isolada por tenant_id como qualquer outra (RLS normal via tenant_isolation), gravada
        // sempre dentro de SetTenantContextAsync (mesmo padrão de SupportAccess/AuditLog).
        builder.HasIndex(h => new { h.TenantId, h.CreatedAt }).HasDatabaseName("idx_tenant_status_history_tenant");

        builder.HasOne(h => h.Tenant).WithMany().HasForeignKey(h => h.TenantId);
    }
}
