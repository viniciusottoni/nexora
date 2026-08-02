using Nexora.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class InventoryCountItemConfiguration : IEntityTypeConfiguration<InventoryCountItem>
{
    public void Configure(EntityTypeBuilder<InventoryCountItem> builder)
    {
        builder.ToTable("inventory_count_item");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.Property(i => i.CountId).HasColumnName("count_id");
        builder.Property(i => i.IngredientId).HasColumnName("ingredient_id");
        builder.Property(i => i.ExpectedQty).HasColumnName("expected_qty").HasColumnType("qty_amount");
        builder.Property(i => i.CountedQty).HasColumnName("counted_qty").HasColumnType("qty_amount");
        builder.Property(i => i.DivergenceQty).HasColumnName("divergence_qty").HasColumnType("qty_amount");
        builder.Property(i => i.UnitCost).HasColumnName("unit_cost").HasColumnType("money_amount");
        builder.Property(i => i.DivergenceCost).HasColumnName("divergence_cost").HasColumnType("money_amount");

        builder.HasIndex(i => new { i.CountId, i.IngredientId })
            .IsUnique()
            .HasDatabaseName("uq_inventory_count_item_count_ingredient");
    }
}
