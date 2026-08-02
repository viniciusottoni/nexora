using Nexora.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class ModifierConfiguration : IEntityTypeConfiguration<Modifier>
{
    public void Configure(EntityTypeBuilder<Modifier> builder)
    {
        builder.ToTable("modifier");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.Property(m => m.GroupId).HasColumnName("group_id");
        builder.Property(m => m.Name).HasColumnName("name").IsRequired();

        // dinheiro — domínio money_amount, tipo C# decimal (ADR-017)
        builder.Property(m => m.PriceDelta).HasColumnName("price_delta").HasColumnType("money_amount").HasDefaultValue(0m);

        builder.Property(m => m.IngredientId).HasColumnName("ingredient_id");

        // quantidade de insumo — domínio qty_amount, NUMERIC(14,4) (ADR-017)
        builder.Property(m => m.Quantity).HasColumnName("quantity").HasColumnType("qty_amount");

        builder.Property(m => m.IsAvailable).HasColumnName("is_available").HasDefaultValue(true);
        builder.Property(m => m.SortOrder).HasColumnName("sort_order").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        // relação ModifierGroup -> Modifiers configurada uma única vez em ModifierGroupConfiguration (lado "um")
    }
}
