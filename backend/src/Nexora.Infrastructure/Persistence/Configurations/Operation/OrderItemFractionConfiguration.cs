using Nexora.Domain.Operation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Operation;

// Espelha o exemplo de 13-Mapeamento-EFCore.md §3 (OrderItemFractionConfiguration) — a entidade
// não tem campos além dos já documentados lá, então a configuração é praticamente idêntica.
internal sealed class OrderItemFractionConfiguration : IEntityTypeConfiguration<OrderItemFraction>
{
    public void Configure(EntityTypeBuilder<OrderItemFraction> builder)
    {
        builder.ToTable("order_item_fraction");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(f => f.TenantId).HasColumnName("tenant_id");
        builder.Property(f => f.OrderItemId).HasColumnName("order_item_id");
        builder.Property(f => f.VariantId).HasColumnName("variant_id");
        builder.Property(f => f.Weight).HasColumnName("weight").HasColumnType("fraction_weight"); // NUMERIC(5,4)
        builder.Property(f => f.UnitPrice).HasColumnName("unit_price").HasColumnType("money_amount");
        builder.Property(f => f.SortOrder).HasColumnName("sort_order").HasColumnType("smallint").HasDefaultValue((short)0);

        // relação OrderItem -> Fractions configurada uma única vez em OrderItemConfiguration (lado "um")
        builder.HasOne(f => f.Variant).WithMany().HasForeignKey(f => f.VariantId);

        builder.HasIndex(f => new { f.TenantId, f.OrderItemId }).HasDatabaseName("idx_order_item_fraction_tenant_item");
    }
}
