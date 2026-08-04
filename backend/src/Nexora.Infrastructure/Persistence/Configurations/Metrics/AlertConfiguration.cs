using Nexora.Domain.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Metrics;

internal sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alert");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.StoreId).HasColumnName("store_id");
        builder.Property(a => a.Type).HasColumnName("type").HasMaxLength(48).IsRequired();
        builder.Property(a => a.Severity).HasColumnName("severity").HasDefaultValue(AlertSeverity.Warning);
        builder.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(32);
        builder.Property(a => a.EntityId).HasColumnName("entity_id");
        builder.Property(a => a.TargetRoles).HasColumnName("target_roles").HasColumnType("text[]");
        builder.Property(a => a.TargetUserId).HasColumnName("target_user_id");
        builder.Property(a => a.Message).HasColumnName("message").IsRequired();
        builder.Property(a => a.Payload).HasColumnName("payload").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(a => a.RaisedAt).HasColumnName("raised_at").HasColumnType("timestamptz");
        builder.Property(a => a.AcknowledgedAt).HasColumnName("acknowledged_at").HasColumnType("timestamptz");
        builder.Property(a => a.AcknowledgedBy).HasColumnName("acknowledged_by");
        builder.Property(a => a.ResolvedAt).HasColumnName("resolved_at").HasColumnType("timestamptz");
        builder.Property(a => a.GroupKey).HasColumnName("group_key");
        builder.Property(a => a.GroupWindowStart).HasColumnName("group_window_start").HasColumnType("timestamptz");
        builder.Property(a => a.PushedAt).HasColumnName("pushed_at").HasColumnType("timestamptz");

        builder.HasIndex(a => new { a.TenantId, a.EntityType, a.EntityId });

        // US-080 §12/E-08: consulta de "abertos" (GET /v1/alerts?status=open) e o motor de
        // avaliação filtram por isto o tempo todo — Docs/Domain/09 idx_alert_open (parcial,
        // só alertas não reconhecidos).
        builder.HasIndex(a => new { a.TenantId, a.StoreId, a.Severity, a.RaisedAt })
            .HasDatabaseName("idx_alert_open")
            .HasFilter("acknowledged_at IS NULL");

        // US-083: agrupamento por tipo+grupo dentro de um tenant/loja — só alertas ainda abertos
        // entram num grupo (um alerta resolvido não deve "puxar" um novo alerta para o grupo
        // antigo). Diferente do uq_alert_group do doc. Domain/09: aqui é DELIBERADAMENTE
        // não-único, porque cada instância individual (um pedido atrasado por vez) precisa da sua
        // própria linha para o detalhamento do grupo (US-083 §7: "alerts": [ {...}, {...} ]).
        builder.HasIndex(a => new { a.TenantId, a.GroupKey })
            .HasDatabaseName("idx_alert_group")
            .HasFilter("resolved_at IS NULL AND group_key IS NOT NULL");
    }
}
