using Nexora.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Sync;

internal sealed class SyncConflictConfiguration : IEntityTypeConfiguration<SyncConflict>
{
    public void Configure(EntityTypeBuilder<SyncConflict> builder)
    {
        builder.ToTable("sync_conflict");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.InstallationId).HasColumnName("installation_id");
        builder.Property(c => c.EventId).HasColumnName("event_id");
        builder.Property(c => c.EventType).HasColumnName("event_type").IsRequired();
        builder.Property(c => c.Reason).HasColumnName("reason").IsRequired();
        builder.Property(c => c.Resolution).HasColumnName("resolution").IsRequired();
        builder.Property(c => c.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(c => c.DetectedAt).HasColumnName("detected_at").HasColumnType("timestamptz");
        builder.Property(c => c.ReviewedBy).HasColumnName("reviewed_by");
        builder.Property(c => c.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("timestamptz");

        builder.HasIndex(c => new { c.TenantId, c.DetectedAt })
            .HasDatabaseName("idx_sync_conflict_tenant_detected_desc")
            .IsDescending(false, true);
    }
}
