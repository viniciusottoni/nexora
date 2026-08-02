using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class DomainEventConfiguration : IEntityTypeConfiguration<DomainEvent>
{
    public void Configure(EntityTypeBuilder<DomainEvent> builder)
    {
        builder.ToTable("domain_event");
        // append-only; particionamento por documento 13 §6 (EventPartitioning) — editado à mão na migration

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.StoreId).HasColumnName("store_id");
        builder.Property(e => e.Type).HasColumnName("type").IsRequired();
        builder.Property(e => e.Version).HasColumnName("version").HasColumnType("smallint").HasDefaultValue((short)1);
        builder.Property(e => e.AggregateType).HasColumnName("aggregate_type").IsRequired();
        builder.Property(e => e.AggregateId).HasColumnName("aggregate_id");
        builder.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(e => e.ActorId).HasColumnName("actor_id");
        builder.Property(e => e.AuthorizedBy).HasColumnName("authorized_by");
        builder.Property(e => e.DeviceId).HasColumnName("device_id");
        builder.Property(e => e.Origin).HasColumnName("origin").IsRequired();
        builder.Property(e => e.DeviceSeq).HasColumnName("device_seq");
        builder.Property(e => e.InstallationId).HasColumnName("installation_id");
        builder.Property(e => e.TraceId).HasColumnName("trace_id").HasMaxLength(32);
        builder.Property(e => e.ClockSuspect).HasColumnName("clock_suspect").HasDefaultValue(false);
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.RecordedAt).HasColumnName("recorded_at").HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.TenantId, e.OccurredAt })
            .HasDatabaseName("idx_domain_event_tenant_time")
            .IsDescending(false, true);
    }
}
