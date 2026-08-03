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
        builder.ToTable("order_item", table =>
        {
            // US-032 (Carimbos de tempo T0 a T5) §3.1/§4 — garantia estrutural de que os seis
            // carimbos nunca ficam fora de ordem cronológica (Docs/Domain/03-Operacao.md
            // "order_item — onde a métrica nasce"): sem esta constraint, um bug de aplicação
            // produziria duração negativa, corrompendo silenciosamente MET-001 a MET-007. Estende
            // o CHECK do DDL canônico (que só garante a EXISTÊNCIA em cadeia, ex. "oven_in_at só
            // pode existir se fired_at existir") com a comparação de HORÁRIO em si — o cenário
            // Gherkin "Ordem cronológica garantida" pede explicitamente que gravar ready_at
            // anterior a fired_at seja recusado pelo banco, o que a versão só-de-existência não
            // cobria.
            table.HasCheckConstraint(
                "ck_item_sequence",
                """
                (fired_at IS NULL OR fired_at >= placed_at)
                AND (oven_in_at IS NULL OR (fired_at IS NOT NULL AND oven_in_at >= fired_at))
                AND (oven_out_at IS NULL OR (oven_in_at IS NOT NULL AND oven_out_at >= oven_in_at))
                AND (ready_at IS NULL OR (fired_at IS NOT NULL AND ready_at >= fired_at AND (oven_out_at IS NULL OR ready_at >= oven_out_at)))
                AND (served_at IS NULL OR (ready_at IS NOT NULL AND served_at >= ready_at))
                """);
        });

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
        // US-032 — autoria de T2/T3, faltava no schema original (só T1/T4/T5 tinham autor).
        builder.Property(i => i.OvenInBy).HasColumnName("oven_in_by");
        builder.Property(i => i.OvenOutBy).HasColumnName("oven_out_by");
        builder.Property(i => i.ReadyBy).HasColumnName("ready_by");
        builder.Property(i => i.ServedBy).HasColumnName("served_by");

        // US-032 (RN-004 "toda ação registra autor, horário e dispositivo") — dispositivo de
        // origem de cada um dos seis carimbos T0 a T5.
        builder.Property(i => i.PlacedDeviceId).HasColumnName("placed_device_id");
        builder.Property(i => i.FiredDeviceId).HasColumnName("fired_device_id");
        builder.Property(i => i.OvenInDeviceId).HasColumnName("oven_in_device_id");
        builder.Property(i => i.OvenOutDeviceId).HasColumnName("oven_out_device_id");
        builder.Property(i => i.ReadyDeviceId).HasColumnName("ready_device_id");
        builder.Property(i => i.ServedDeviceId).HasColumnName("served_device_id");

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
