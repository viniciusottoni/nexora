using Nexora.Domain.Cashier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Cashier;

internal sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("payment_allocation");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.PaymentId).HasColumnName("payment_id");
        builder.Property(a => a.OrderId).HasColumnName("order_id");
        builder.Property(a => a.Amount).HasColumnName("amount").HasColumnType("money_amount");

        builder.HasIndex(a => new { a.PaymentId, a.OrderId }).IsUnique().HasDatabaseName("uq_payment_allocation_payment_order");
        builder.HasIndex(a => new { a.TenantId, a.OrderId });
    }
}
