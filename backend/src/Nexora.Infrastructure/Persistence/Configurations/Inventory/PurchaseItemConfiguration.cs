using Nexora.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("purchase_item");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.Property(i => i.PurchaseId).HasColumnName("purchase_id");
        builder.Property(i => i.IngredientId).HasColumnName("ingredient_id");
        builder.Property(i => i.Quantity).HasColumnName("quantity").HasColumnType("qty_amount");
        builder.Property(i => i.UomCode).HasColumnName("uom_code").HasMaxLength(8).IsRequired();
        builder.Property(i => i.UnitCost).HasColumnName("unit_cost").HasColumnType("money_amount");
        builder.Property(i => i.TotalCost).HasColumnName("total_cost").HasColumnType("money_amount");
        builder.Property(i => i.ExpiresAt).HasColumnName("expires_at").HasColumnType("date");
        builder.Property(i => i.LotCode).HasColumnName("lot_code").HasMaxLength(40);

        builder.HasIndex(i => new { i.TenantId, i.PurchaseId });
    }
}
