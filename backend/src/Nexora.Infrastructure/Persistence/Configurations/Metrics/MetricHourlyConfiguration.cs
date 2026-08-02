using Nexora.Domain.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Metrics;

internal sealed class MetricHourlyConfiguration : IEntityTypeConfiguration<MetricHourly>
{
    public void Configure(EntityTypeBuilder<MetricHourly> builder)
    {
        builder.ToTable("metric_hourly");

        // Chave composta — MetricHourly não tem coluna id própria (documento 13, §3).
        builder.HasKey(m => new { m.TenantId, m.StoreId, m.Hour, m.Channel });

        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.Property(m => m.StoreId).HasColumnName("store_id");
        builder.Property(m => m.Hour).HasColumnName("hour").HasColumnType("timestamptz");
        builder.Property(m => m.BusinessDay).HasColumnName("business_day").HasColumnType("date");
        builder.Property(m => m.Channel).HasColumnName("channel");
        builder.Property(m => m.Orders).HasColumnName("orders").HasDefaultValue(0);
        builder.Property(m => m.OrdersCancelled).HasColumnName("orders_cancelled").HasDefaultValue(0);
        builder.Property(m => m.Items).HasColumnName("items").HasDefaultValue(0);
        builder.Property(m => m.ItemsRefired).HasColumnName("items_refired").HasDefaultValue(0);
        builder.Property(m => m.Revenue).HasColumnName("revenue").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.AvgQueueSeconds).HasColumnName("avg_queue_seconds");
        builder.Property(m => m.AvgPrepSeconds).HasColumnName("avg_prep_seconds");
        builder.Property(m => m.AvgCookSeconds).HasColumnName("avg_cook_seconds");
        builder.Property(m => m.AvgExpediteSeconds).HasColumnName("avg_expedite_seconds");
        builder.Property(m => m.AvgTotalSeconds).HasColumnName("avg_total_seconds");
        builder.Property(m => m.P90TotalSeconds).HasColumnName("p90_total_seconds");
        builder.Property(m => m.MaxTotalSeconds).HasColumnName("max_total_seconds");
        builder.Property(m => m.OnTimeCount).HasColumnName("on_time_count").HasDefaultValue(0);
        builder.Property(m => m.LateCount).HasColumnName("late_count").HasDefaultValue(0);
        builder.Property(m => m.OvenBusySeconds).HasColumnName("oven_busy_seconds").HasDefaultValue(0);
        builder.Property(m => m.OvenIdleWithQueueSeconds).HasColumnName("oven_idle_with_queue_seconds").HasDefaultValue(0);
        builder.Property(m => m.ComputedAt).HasColumnName("computed_at").HasColumnType("timestamptz");

        builder.HasIndex(m => new { m.TenantId, m.BusinessDay });
    }
}
