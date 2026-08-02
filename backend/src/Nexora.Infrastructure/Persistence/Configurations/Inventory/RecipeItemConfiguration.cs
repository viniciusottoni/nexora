using Nexora.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class RecipeItemConfiguration : IEntityTypeConfiguration<RecipeItem>
{
    public void Configure(EntityTypeBuilder<RecipeItem> builder)
    {
        builder.ToTable("recipe_item");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.Property(i => i.RecipeId).HasColumnName("recipe_id");
        builder.Property(i => i.IngredientId).HasColumnName("ingredient_id");
        builder.Property(i => i.SubRecipeId).HasColumnName("sub_recipe_id");
        builder.Property(i => i.Quantity).HasColumnName("quantity").HasColumnType("qty_amount");
        builder.Property(i => i.UomCode).HasColumnName("uom_code").HasMaxLength(8).IsRequired();
        builder.Property(i => i.WastePercent).HasColumnName("waste_percent").HasColumnType("percent_amount").HasDefaultValue(0m);
        builder.Property(i => i.SortOrder).HasColumnName("sort_order").HasColumnType("smallint").HasDefaultValue((short)0);

        // Relação com o Recipe "dono" (RecipeId) é configurada em RecipeConfiguration
        // (HasMany(r => r.Items)); aqui só a auto-referência opcional para sub-receita.
        builder.HasOne<Recipe>().WithMany().HasForeignKey(i => i.SubRecipeId);

        builder.HasIndex(i => new { i.TenantId, i.RecipeId });
    }
}
