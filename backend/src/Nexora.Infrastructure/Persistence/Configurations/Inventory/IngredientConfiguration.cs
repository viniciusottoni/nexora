using Nexora.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.ToTable("ingredient");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.Property(i => i.Name).HasColumnName("name").IsRequired();
        builder.Property(i => i.Category).HasColumnName("category").HasMaxLength(40);
        builder.Property(i => i.UomCode).HasColumnName("uom_code").HasMaxLength(8).IsRequired();
        builder.Property(i => i.SupplierId).HasColumnName("supplier_id");

        builder.Property(i => i.AvgCost).HasColumnName("avg_cost").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(i => i.LastCost).HasColumnName("last_cost").HasColumnType("money_amount");
        builder.Property(i => i.CurrentStock).HasColumnName("current_stock").HasColumnType("qty_amount").HasDefaultValue(0m);
        builder.Property(i => i.StockSyncedAt).HasColumnName("stock_synced_at").HasColumnType("timestamptz");
        builder.Property(i => i.MinStock).HasColumnName("min_stock").HasColumnType("qty_amount").HasDefaultValue(0m);

        builder.Property(i => i.IsPerishable).HasColumnName("is_perishable").HasDefaultValue(false);
        builder.Property(i => i.ShelfLifeDays).HasColumnName("shelf_life_days").HasColumnType("smallint");
        builder.Property(i => i.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(i => i.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasOne<Supplier>().WithMany().HasForeignKey(i => i.SupplierId);
        builder.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(i => i.UomCode).HasPrincipalKey(u => u.Code);
    }
}
