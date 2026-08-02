using Nexora.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movement");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.Property(m => m.StoreId).HasColumnName("store_id");
        builder.Property(m => m.IngredientId).HasColumnName("ingredient_id");
        builder.Property(m => m.BusinessDay).HasColumnName("business_day").HasColumnType("date");
        builder.Property(m => m.Type).HasColumnName("type");
        builder.Property(m => m.Quantity).HasColumnName("quantity").HasColumnType("qty_amount");
        builder.Property(m => m.UomCode).HasColumnName("uom_code").HasMaxLength(8).IsRequired();
        builder.Property(m => m.UnitCost).HasColumnName("unit_cost").HasColumnType("money_amount");
        builder.Property(m => m.TotalCost).HasColumnName("total_cost").HasColumnType("money_amount");
        builder.Property(m => m.ReferenceType).HasColumnName("reference_type").HasMaxLength(32);
        builder.Property(m => m.ReferenceId).HasColumnName("reference_id");
        builder.Property(m => m.WasteReason).HasColumnName("waste_reason");
        builder.Property(m => m.Reason).HasColumnName("reason");

        builder.Property(m => m.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(m => m.CreatedBy).HasColumnName("created_by");
        builder.Property(m => m.AuthorizedBy).HasColumnName("authorized_by");

        builder.HasIndex(m => new { m.TenantId, m.IngredientId, m.OccurredAt })
            .HasDatabaseName("idx_stock_movement_ingredient")
            .IsDescending(false, false, true);

        builder.HasIndex(m => new { m.TenantId, m.BusinessDay, m.Type });
    }
}
