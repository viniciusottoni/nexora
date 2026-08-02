using Nexora.Domain.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Metrics;

internal sealed class MetricDailyConfiguration : IEntityTypeConfiguration<MetricDaily>
{
    public void Configure(EntityTypeBuilder<MetricDaily> builder)
    {
        builder.ToTable("metric_daily");

        // Chave composta — MetricDaily não tem coluna id própria (documento 13, §3).
        builder.HasKey(m => new { m.TenantId, m.StoreId, m.BusinessDay, m.Channel });

        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.Property(m => m.StoreId).HasColumnName("store_id");
        builder.Property(m => m.BusinessDay).HasColumnName("business_day").HasColumnType("date");
        builder.Property(m => m.Channel).HasColumnName("channel");
        builder.Property(m => m.Orders).HasColumnName("orders").HasDefaultValue(0);
        builder.Property(m => m.OrdersCancelled).HasColumnName("orders_cancelled").HasDefaultValue(0);
        builder.Property(m => m.Items).HasColumnName("items").HasDefaultValue(0);
        builder.Property(m => m.Revenue).HasColumnName("revenue").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.Discounts).HasColumnName("discounts").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.ServiceFee).HasColumnName("service_fee").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.AvgTicket).HasColumnName("avg_ticket").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.Covers).HasColumnName("covers").HasDefaultValue(0);
        builder.Property(m => m.Sessions).HasColumnName("sessions").HasDefaultValue(0);
        builder.Property(m => m.TableTurns).HasColumnName("table_turns").HasColumnType("numeric(6,2)");
        builder.Property(m => m.AvgStaySeconds).HasColumnName("avg_stay_seconds");
        builder.Property(m => m.AvgTotalSeconds).HasColumnName("avg_total_seconds");
        builder.Property(m => m.P90TotalSeconds).HasColumnName("p90_total_seconds");
        builder.Property(m => m.OnTimeRate).HasColumnName("on_time_rate").HasColumnType("numeric(5,4)");
        builder.Property(m => m.CmvTheoretical).HasColumnName("cmv_theoretical").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.LaborCost).HasColumnName("labor_cost").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.CardFees).HasColumnName("card_fees").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.ComputedAt).HasColumnName("computed_at").HasColumnType("timestamptz");
    }
}
