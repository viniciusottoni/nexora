using Nexora.Domain.Operation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Operation;

// Baseado no exemplo de 13-Mapeamento-EFCore.md §3 (OrderConfiguration), estendido com todos os
// campos do schema.prisma real que o exemplo do doc omitiu por brevidade (notes, cancel_reason,
// cancelled_by, authorized_by, chaves fiscais, created_by, device_id, customer_id, address_id,
// courier_id, service_fee_amount, closed_at, cancelled_at).
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("order"); // "order" é palavra reservada — aspas resolvidas pelo provider Npgsql automaticamente

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(o => o.TenantId).HasColumnName("tenant_id");
        builder.Property(o => o.StoreId).HasColumnName("store_id");
        builder.Property(o => o.SessionId).HasColumnName("session_id");
        builder.Property(o => o.Channel).HasColumnName("channel");
        builder.Property(o => o.ShortCode).HasColumnName("short_code").HasMaxLength(8).IsRequired();

        // business_day materializado na escrita — nunca calculado em tempo de consulta (ADR-018)
        builder.Property(o => o.BusinessDay).HasColumnName("business_day").HasColumnType("date");

        builder.Property(o => o.Status).HasColumnName("status").HasDefaultValue(OrderStatus.Draft);

        builder.Property(o => o.CustomerId).HasColumnName("customer_id");
        builder.Property(o => o.AddressId).HasColumnName("address_id");
        builder.Property(o => o.CourierId).HasColumnName("courier_id");

        // carimbos de tempo — origem da métrica (ADR-006)
        builder.Property(o => o.PlacedAt).HasColumnName("placed_at").HasColumnType("timestamptz");
        builder.Property(o => o.FirstFiredAt).HasColumnName("first_fired_at").HasColumnType("timestamptz");
        builder.Property(o => o.ReadyAt).HasColumnName("ready_at").HasColumnType("timestamptz");
        builder.Property(o => o.DispatchedAt).HasColumnName("dispatched_at").HasColumnType("timestamptz");
        builder.Property(o => o.ServedAt).HasColumnName("served_at").HasColumnType("timestamptz");
        builder.Property(o => o.ClosedAt).HasColumnName("closed_at").HasColumnType("timestamptz");
        builder.Property(o => o.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamptz");
        builder.Property(o => o.PromisedAt).HasColumnName("promised_at").HasColumnType("timestamptz");

        // dinheiro — domínio money_amount, tipo C# decimal (ADR-017)
        builder.Property(o => o.Subtotal).HasColumnName("subtotal").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(o => o.DiscountAmount).HasColumnName("discount_amount").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(o => o.DeliveryFee).HasColumnName("delivery_fee").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(o => o.ServiceFeeAmount).HasColumnName("service_fee_amount").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(o => o.Total).HasColumnName("total").HasColumnType("money_amount").HasDefaultValue(0m);

        builder.Property(o => o.Notes).HasColumnName("notes");
        builder.Property(o => o.CancelReason).HasColumnName("cancel_reason");
        builder.Property(o => o.CancelledBy).HasColumnName("cancelled_by");
        builder.Property(o => o.AuthorizedBy).HasColumnName("authorized_by");

        builder.Property(o => o.FiscalStatus).HasColumnName("fiscal_status").HasDefaultValue(FiscalStatus.None);
        builder.Property(o => o.FiscalKey).HasColumnName("fiscal_key").HasMaxLength(44);
        builder.Property(o => o.FiscalNumber).HasColumnName("fiscal_number");
        builder.Property(o => o.FiscalSeries).HasColumnName("fiscal_series").HasColumnType("smallint");
        builder.Property(o => o.FiscalProtocol).HasColumnName("fiscal_protocol").HasMaxLength(32);

        builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(o => o.CreatedBy).HasColumnName("created_by");
        builder.Property(o => o.DeviceId).HasColumnName("device_id");

        // relação TableSession -> Orders configurada uma única vez em TableSessionConfiguration (lado "um")
        builder.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.StoreId, o.BusinessDay, o.ShortCode })
            .IsUnique()
            .HasDatabaseName("uq_order_short_code");

        builder.HasIndex(o => new { o.TenantId, o.BusinessDay, o.Channel });

        builder.HasIndex(o => new { o.TenantId, o.PlacedAt })
            .HasDatabaseName("idx_order_placed_desc")
            .IsDescending(false, true);
    }
}
