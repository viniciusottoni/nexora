using Nexora.Domain.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Delivery;

internal sealed class DeliveryRunConfiguration : IEntityTypeConfiguration<DeliveryRun>
{
    public void Configure(EntityTypeBuilder<DeliveryRun> builder)
    {
        builder.ToTable("delivery_run");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.TenantId).HasColumnName("tenant_id");
        builder.Property(r => r.StoreId).HasColumnName("store_id");
        builder.Property(r => r.CourierId).HasColumnName("courier_id");
        builder.Property(r => r.BusinessDay).HasColumnName("business_day").HasColumnType("date");
        builder.Property(r => r.ArrivedAt).HasColumnName("arrived_at").HasColumnType("timestamptz");
        builder.Property(r => r.DispatchedAt).HasColumnName("dispatched_at").HasColumnType("timestamptz");
        builder.Property(r => r.ReturnedAt).HasColumnName("returned_at").HasColumnType("timestamptz");
        builder.Property(r => r.StopsCount).HasColumnName("stops_count").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(r => r.DistanceKm).HasColumnName("distance_km").HasColumnType("numeric(8,2)");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
    }
}
