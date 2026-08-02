using Nexora.Domain.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Metrics;

internal sealed class MetricOperatorDailyConfiguration : IEntityTypeConfiguration<MetricOperatorDaily>
{
    public void Configure(EntityTypeBuilder<MetricOperatorDaily> builder)
    {
        builder.ToTable("metric_operator_daily");

        // Chave composta — MetricOperatorDaily não tem coluna id própria (documento 13, §3).
        builder.HasKey(m => new { m.TenantId, m.StoreId, m.UserId, m.BusinessDay, m.RoleContext });

        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.Property(m => m.StoreId).HasColumnName("store_id");
        builder.Property(m => m.UserId).HasColumnName("user_id");
        builder.Property(m => m.BusinessDay).HasColumnName("business_day").HasColumnType("date");
        builder.Property(m => m.RoleContext).HasColumnName("role_context").HasMaxLength(16).IsRequired();
        builder.Property(m => m.Orders).HasColumnName("orders").HasDefaultValue(0);
        builder.Property(m => m.Items).HasColumnName("items").HasDefaultValue(0);
        builder.Property(m => m.Revenue).HasColumnName("revenue").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.AvgTicket).HasColumnName("avg_ticket").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.Sessions).HasColumnName("sessions").HasDefaultValue(0);
        builder.Property(m => m.AvgServeSeconds).HasColumnName("avg_serve_seconds");
        builder.Property(m => m.UpsellOffered).HasColumnName("upsell_offered").HasDefaultValue(0);
        builder.Property(m => m.UpsellAccepted).HasColumnName("upsell_accepted").HasDefaultValue(0);
        builder.Property(m => m.Cancellations).HasColumnName("cancellations").HasDefaultValue(0);
        builder.Property(m => m.DiscountsGiven).HasColumnName("discounts_given").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.ComputedAt).HasColumnName("computed_at").HasColumnType("timestamptz");
    }
}
