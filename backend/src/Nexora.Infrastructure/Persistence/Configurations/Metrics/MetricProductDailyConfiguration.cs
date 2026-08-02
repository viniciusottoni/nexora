using Nexora.Domain.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Metrics;

internal sealed class MetricProductDailyConfiguration : IEntityTypeConfiguration<MetricProductDaily>
{
    public void Configure(EntityTypeBuilder<MetricProductDaily> builder)
    {
        builder.ToTable("metric_product_daily");

        // Chave composta — MetricProductDaily não tem coluna id própria (documento 13, §3).
        builder.HasKey(m => new { m.TenantId, m.StoreId, m.VariantId, m.BusinessDay });

        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.Property(m => m.StoreId).HasColumnName("store_id");
        builder.Property(m => m.VariantId).HasColumnName("variant_id");
        builder.Property(m => m.BusinessDay).HasColumnName("business_day").HasColumnType("date");
        builder.Property(m => m.Quantity).HasColumnName("quantity").HasDefaultValue(0);
        builder.Property(m => m.FractionQuantity).HasColumnName("fraction_quantity").HasColumnType("numeric(10,4)").HasDefaultValue(0m);
        builder.Property(m => m.Revenue).HasColumnName("revenue").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.Cost).HasColumnName("cost").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(m => m.Margin).HasColumnName("margin").HasColumnType("money_amount");
        builder.Property(m => m.AvgPrepSeconds).HasColumnName("avg_prep_seconds");
        builder.Property(m => m.Cancelled).HasColumnName("cancelled").HasDefaultValue(0);
        builder.Property(m => m.Refired).HasColumnName("refired").HasDefaultValue(0);
        builder.Property(m => m.ComputedAt).HasColumnName("computed_at").HasColumnType("timestamptz");

        builder.HasIndex(m => new { m.TenantId, m.BusinessDay });
    }
}
