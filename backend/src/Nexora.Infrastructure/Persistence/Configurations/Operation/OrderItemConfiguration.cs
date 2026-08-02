using Nexora.Domain.Operation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Operation;

// Baseado no exemplo de 13-Mapeamento-EFCore.md §3 (OrderItemConfiguration), estendido com todos
// os campos do schema.prisma real que o exemplo do doc omitiu por brevidade (modifiers_total,
// notes, oven_slot, priority_score, cancel_reason, cancelled_by, authorized_by, refire_of_id,
// refire_reason, fired_by, ready_by, served_by).
internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_item");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.Property(i => i.OrderId).HasColumnName("order_id");
        builder.Property(i => i.VariantId).HasColumnName("variant_id");
        builder.Property(i => i.StationId).HasColumnName("station_id");

        builder.Property(i => i.Quantity).HasColumnName("quantity").HasColumnType("smallint").HasDefaultValue((short)1);
        builder.Property(i => i.UnitPrice).HasColumnName("unit_price").HasColumnType("money_amount");
        builder.Property(i => i.ModifiersTotal).HasColumnName("modifiers_total").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(i => i.TotalPrice).HasColumnName("total_price").HasColumnType("money_amount");
        builder.Property(i => i.UnitCost).HasColumnName("unit_cost").HasColumnType("money_amount");

        builder.Property(i => i.Status).HasColumnName("status").HasDefaultValue(OrderItemStatus.Queued);
        builder.Property(i => i.Notes).HasColumnName("notes");

        builder.Property(i => i.PlacedAt).HasColumnName("placed_at").HasColumnType("timestamptz");
        builder.Property(i => i.FireAt).HasColumnName("fire_at").HasColumnType("timestamptz");
        builder.Property(i => i.FiredAt).HasColumnName("fired_at").HasColumnType("timestamptz");
        builder.Property(i => i.OvenInAt).HasColumnName("oven_in_at").HasColumnType("timestamptz");
        builder.Property(i => i.OvenOutAt).HasColumnName("oven_out_at").HasColumnType("timestamptz");
        builder.Property(i => i.ReadyAt).HasColumnName("ready_at").HasColumnType("timestamptz");
        builder.Property(i => i.ServedAt).HasColumnName("served_at").HasColumnType("timestamptz");

        builder.Property(i => i.OvenSlot).HasColumnName("oven_slot").HasColumnType("smallint");
        builder.Property(i => i.PriorityScore).HasColumnName("priority_score");
        builder.Property(i => i.CancelReason).HasColumnName("cancel_reason");
        builder.Property(i => i.CancelledBy).HasColumnName("cancelled_by");
        builder.Property(i => i.AuthorizedBy).HasColumnName("authorized_by");
        builder.Property(i => i.RefireOfId).HasColumnName("refire_of_id");
        builder.Property(i => i.RefireReason).HasColumnName("refire_reason");
        // US-028 (Repetir item com um toque) — coluna adicional ao DDL original de US-024/03-Operacao.md.
        builder.Property(i => i.RepeatedFromItemId).HasColumnName("repeated_from_item_id");
        builder.Property(i => i.FiredBy).HasColumnName("fired_by");
        builder.Property(i => i.ReadyBy).HasColumnName("ready_by");
        builder.Property(i => i.ServedBy).HasColumnName("served_by");

        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        // relação Order -> Items configurada uma única vez em OrderConfiguration (lado "um")
        builder.HasOne(i => i.Variant).WithMany().HasForeignKey(i => i.VariantId);

        // auto-referência do refogo — não é uma FK obrigatória do EF (RefireOfId aponta para outro OrderItem)
        builder.HasOne<OrderItem>().WithMany().HasForeignKey(i => i.RefireOfId).OnDelete(DeleteBehavior.Restrict);

        // auto-referência da repetição (US-028) — mesmo padrão do refogo acima.
        builder.HasOne<OrderItem>().WithMany().HasForeignKey(i => i.RepeatedFromItemId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Fractions).WithOne(f => f.Item).HasForeignKey(f => f.OrderItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.Modifiers).WithOne(m => m.Item).HasForeignKey(m => m.OrderItemId).OnDelete(DeleteBehavior.Cascade);

        // NOTA: filtro em valores inteiros (não 'QUEUED','FIRED',...) pelo mesmo motivo
        // documentado em AppUserConfiguration — OrderItemStatus ainda é gravado como integer
        // (Queued=0, Fired=1, InOven=2, OutOfOven=3; ver Nexora.Domain.Operation.Enums.OrderItemStatus).
        builder.HasIndex(i => new { i.TenantId, i.StationId, i.Status, i.PlacedAt })
            .HasDatabaseName("idx_item_queue")
            .HasFilter("status IN (0,1,2,3)"); // índice parcial — ADR-038 §1

        builder.HasIndex(i => i.OrderId);
        builder.HasIndex(i => new { i.TenantId, i.VariantId, i.PlacedAt });
    }
}
