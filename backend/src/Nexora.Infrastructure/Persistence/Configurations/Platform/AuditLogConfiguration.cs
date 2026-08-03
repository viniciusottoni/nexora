using System.Net;
using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log");
        // append-only (E-09/US-090): UPDATE/DELETE revogados do papel app_user_role por permissão
        // de banco (migration PartitionAuditLogAndRestrictMutation) — a tabela também é
        // particionada por occurred_at (ADR-035) nessa mesma migration, editada à mão em SQL cru
        // porque o EF Core não expressa particionamento nativamente (Docs/Domain/13 §1).

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.StoreId).HasColumnName("store_id");
        builder.Property(a => a.ActorId).HasColumnName("actor_id");
        builder.Property(a => a.AuthorizedBy).HasColumnName("authorized_by");
        builder.Property(a => a.DeviceId).HasColumnName("device_id");
        builder.Property(a => a.Action).HasColumnName("action").IsRequired();
        builder.Property(a => a.Entity).HasColumnName("entity").IsRequired();
        builder.Property(a => a.EntityId).HasColumnName("entity_id");
        builder.Property(a => a.Before).HasColumnName("before").HasColumnType("jsonb");
        builder.Property(a => a.After).HasColumnName("after").HasColumnType("jsonb");
        builder.Property(a => a.Reason).HasColumnName("reason");
        // Domain mantém Ip como string (nota da classe) para não acoplar Nexora.Domain a
        // System.Net (ADR-039); o provider Npgsql só aceita gravar em coluna "inet" a partir de
        // IPAddress/NpgsqlInet — conversor explícito faz a ponte sem mudar o tipo no domínio.
        builder.Property(a => a.Ip).HasColumnName("ip").HasColumnType("inet").HasConversion(
            new ValueConverter<string?, IPAddress?>(
                v => v == null ? null : IPAddress.Parse(v),
                v => v == null ? null : v.ToString()));
        builder.Property(a => a.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz").IsRequired(); // ADR-034
        builder.Property(a => a.RecordedAt).HasColumnName("recorded_at").HasColumnType("timestamptz");
        builder.Property(a => a.TraceId).HasColumnName("trace_id").HasMaxLength(32);
        builder.Property(a => a.DomainEventId).HasColumnName("domain_event_id");

        builder.HasIndex(a => new { a.TenantId, a.OccurredAt })
            .HasDatabaseName("idx_audit_tenant_time")
            .IsDescending(false, true);

        builder.HasIndex(a => new { a.TenantId, a.Entity, a.EntityId }).HasDatabaseName("idx_audit_entity");

        builder.HasIndex(a => new { a.TenantId, a.ActorId, a.OccurredAt })
            .HasDatabaseName("idx_audit_actor")
            .IsDescending(false, false, true);

        builder.HasIndex(a => new { a.TenantId, a.Action, a.OccurredAt })
            .HasDatabaseName("idx_audit_action")
            .IsDescending(false, false, true);

        builder.HasOne(a => a.Tenant).WithMany(t => t.AuditLogs).HasForeignKey(a => a.TenantId);
    }
}
