using Nexora.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipe");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.TenantId).HasColumnName("tenant_id");
        builder.Property(r => r.VariantId).HasColumnName("variant_id");
        builder.Property(r => r.Name).HasColumnName("name");
        builder.Property(r => r.IsSubRecipe).HasColumnName("is_sub_recipe").HasDefaultValue(false);
        builder.Property(r => r.YieldQty).HasColumnName("yield_qty").HasColumnType("qty_amount").HasDefaultValue(1m);
        builder.Property(r => r.YieldUom).HasColumnName("yield_uom").HasMaxLength(8);
        builder.Property(r => r.Notes).HasColumnName("notes");

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasMany(r => r.Items).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
    }
}
