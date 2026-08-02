using Nexora.Domain.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Metrics;

internal sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("goal");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(g => g.TenantId).HasColumnName("tenant_id");
        builder.Property(g => g.StoreId).HasColumnName("store_id");
        builder.Property(g => g.MetricCode).HasColumnName("metric_code").HasMaxLength(40).IsRequired();
        builder.Property(g => g.TargetValue).HasColumnName("target_value").HasColumnType("numeric(14,4)");
        builder.Property(g => g.Comparison).HasColumnName("comparison").HasMaxLength(8).HasDefaultValue("LTE");
        builder.Property(g => g.Period).HasColumnName("period").HasMaxLength(10).IsRequired();
        builder.Property(g => g.ValidFrom).HasColumnName("valid_from").HasColumnType("date");
        builder.Property(g => g.ValidTo).HasColumnName("valid_to").HasColumnType("date");
        builder.Property(g => g.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(g => g.CreatedBy).HasColumnName("created_by");

        builder.HasIndex(g => new { g.TenantId, g.StoreId, g.MetricCode, g.Period, g.ValidFrom })
            .IsUnique()
            .HasDatabaseName("uq_goal_tenant_store_metric_period_valid_from");
    }
}
