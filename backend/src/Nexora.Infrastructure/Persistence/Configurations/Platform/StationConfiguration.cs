using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class StationConfiguration : IEntityTypeConfiguration<Station>
{
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.ToTable("station");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.StoreId).HasColumnName("store_id");
        builder.Property(s => s.Code).HasColumnName("code").HasMaxLength(32).IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").IsRequired();
        builder.Property(s => s.Type).HasColumnName("type").HasDefaultValue(StationType.Assembly);
        builder.Property(s => s.CapacitySlots).HasColumnName("capacity_slots").HasColumnType("smallint");
        builder.Property(s => s.AvgCookSeconds).HasColumnName("avg_cook_seconds");
        builder.Property(s => s.Color).HasColumnName("color").HasMaxLength(16);
        builder.Property(s => s.IsBottleneck).HasColumnName("is_bottleneck").HasDefaultValue(false);
        builder.Property(s => s.SortOrder).HasColumnName("sort_order").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.TenantId, s.Code }).IsUnique().HasDatabaseName("uq_station_code");
        builder.HasIndex(s => new { s.TenantId, s.StoreId }, "IX_Station_TenantStore")
            .HasDatabaseName("idx_station_tenant_store");
        builder.HasIndex(s => new { s.TenantId, s.StoreId }, "IX_Station_CurrentBottleneck")
            .HasDatabaseName("uq_station_current_bottleneck_tenant_store")
            .IsUnique()
            .HasFilter("deleted_at IS NULL AND is_bottleneck = TRUE");

        builder.HasOne(s => s.Store).WithMany(st => st.Stations).HasForeignKey(s => s.StoreId);
    }
}
