using Nexora.Domain.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Delivery;

internal sealed class DeliveryZoneConfiguration : IEntityTypeConfiguration<DeliveryZone>
{
    public void Configure(EntityTypeBuilder<DeliveryZone> builder)
    {
        builder.ToTable("delivery_zone");

        builder.HasKey(z => z.Id);
        builder.Property(z => z.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(z => z.TenantId).HasColumnName("tenant_id");
        builder.Property(z => z.StoreId).HasColumnName("store_id");
        builder.Property(z => z.Name).HasColumnName("name").IsRequired();
        builder.Property(z => z.Geometry).HasColumnName("geometry").HasColumnType("jsonb");
        builder.Property(z => z.Districts).HasColumnName("districts").HasColumnType("text[]");
        builder.Property(z => z.Fee).HasColumnName("fee").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(z => z.MinOrder).HasColumnName("min_order").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(z => z.AvgMinutes).HasColumnName("avg_minutes").HasColumnType("smallint").HasDefaultValue(20);
        builder.Property(z => z.MaxDistanceKm).HasColumnName("max_distance_km").HasColumnType("numeric(6,2)");
        builder.Property(z => z.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(z => z.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(z => z.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(z => z.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
    }
}
