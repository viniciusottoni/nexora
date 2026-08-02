using Nexora.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Sync;

internal sealed class OutboxConfiguration : IEntityTypeConfiguration<Outbox>
{
    public void Configure(EntityTypeBuilder<Outbox> builder)
    {
        builder.ToTable("outbox");

        // Chave composta — Outbox não tem coluna id própria (documento 13, §3).
        builder.HasKey(o => new { o.EventId, o.OccurredAt });

        builder.Property(o => o.EventId).HasColumnName("event_id");
        builder.Property(o => o.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz");
        builder.Property(o => o.TenantId).HasColumnName("tenant_id");
        builder.Property(o => o.DeviceSeq).HasColumnName("device_seq");
        builder.Property(o => o.Status).HasColumnName("status").HasMaxLength(16).HasDefaultValue("PENDING");
        builder.Property(o => o.Attempts).HasColumnName("attempts").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(o => o.LastError).HasColumnName("last_error");
        builder.Property(o => o.NextRetryAt).HasColumnName("next_retry_at").HasColumnType("timestamptz");
        builder.Property(o => o.SyncedAt).HasColumnName("synced_at").HasColumnType("timestamptz");
        builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
    }
}
