using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

/// <summary>US-157 · Central operacional, auditoria e atalhos de suporte — reconhecimento append-only de pendência da fila de atenção.</summary>
internal sealed class AdministrativeAttentionAcknowledgementConfiguration : IEntityTypeConfiguration<AdministrativeAttentionAcknowledgement>
{
    public void Configure(EntityTypeBuilder<AdministrativeAttentionAcknowledgement> builder)
    {
        builder.ToTable("administrative_attention_acknowledgement");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(a => a.ItemType).HasColumnName("item_type").IsRequired();
        builder.Property(a => a.Reason).HasColumnName("reason").IsRequired();
        builder.Property(a => a.ActorId).HasColumnName("actor_id");
        builder.Property(a => a.AcknowledgedAt).HasColumnName("acknowledged_at").HasColumnType("timestamptz");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        // Tabela de negócio comum com tenant_id — isolada por RLS (tenant_isolation), mesmo padrão
        // de ownership_transfer/tenant_status_history. O índice cobre a checagem "esta condição já
        // foi reconhecida depois do instante em que começou" (GetAttentionQueueQueryHandler) sem
        // table scan por tenant.
        builder.HasIndex(a => new { a.TenantId, a.ItemId, a.AcknowledgedAt })
            .HasDatabaseName("idx_administrative_attention_ack_tenant_item");

        builder.HasOne(a => a.Tenant).WithMany().HasForeignKey(a => a.TenantId);
    }
}
