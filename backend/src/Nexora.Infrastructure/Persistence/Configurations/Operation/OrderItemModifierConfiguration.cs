using Nexora.Domain.Operation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Operation;

internal sealed class OrderItemModifierConfiguration : IEntityTypeConfiguration<OrderItemModifier>
{
    public void Configure(EntityTypeBuilder<OrderItemModifier> builder)
    {
        builder.ToTable("order_item_modifier");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.Property(m => m.OrderItemId).HasColumnName("order_item_id");
        builder.Property(m => m.ModifierId).HasColumnName("modifier_id");
        builder.Property(m => m.Quantity).HasColumnName("quantity").HasColumnType("smallint").HasDefaultValue((short)1);

        // dinheiro — domínio money_amount, tipo C# decimal (ADR-017)
        builder.Property(m => m.PriceDelta).HasColumnName("price_delta").HasColumnType("money_amount").HasDefaultValue(0m);

        builder.Property(m => m.NameSnapshot).HasColumnName("name_snapshot").IsRequired();

        // relação OrderItem -> Modifiers configurada uma única vez em OrderItemConfiguration (lado "um")

        builder.HasIndex(m => new { m.TenantId, m.OrderItemId }).HasDatabaseName("idx_order_item_modifier_tenant_item");
    }
}
