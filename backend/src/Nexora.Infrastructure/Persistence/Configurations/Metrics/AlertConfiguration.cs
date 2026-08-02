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

        builder.HasIndex(a => new { a.TenantId, a.EntityType, a.EntityId });
    }
}
